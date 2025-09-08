#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class Debug_JsonRebuilder : EditorWindow
{
    // UI state
    private string inputJsonPath = "";

    // --- Minimal mirrors just to pull floorplan.base64 from the JSON ---
    [Serializable] private class ExportSessionData_Min
    {
        public ExportFloorplanData_Min floorplan;
    }
    [Serializable] private class ExportFloorplanData_Min
    {
        public string base64;
        public Vector2 uvScale;   // tiling from JSON
        public Vector2 uvOffset;  // offset from JSON
        public Vector2Int imageDimensions; // source image size
    }
    // -------------------------------------------------------------------

    [MenuItem("Roomy/Debug/JSON Reassembler")]
    public static void Open() => GetWindow<Debug_JsonRebuilder>("JSON Reassembler");

    private void OnGUI()
    {
        GUILayout.Label("Rebuild Floorplan + Furniture PNGs from uploaded JSON", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // Path field
        EditorGUILayout.BeginHorizontal();
        inputJsonPath = EditorGUILayout.TextField("JSON Path", inputJsonPath);
        if (GUILayout.Button("Pick…", GUILayout.Width(70)))
        {
            var picked = EditorUtility.OpenFilePanel("Select session JSON", GetProjectRoot(), "json");
            if (!string.IsNullOrEmpty(picked)) inputJsonPath = MakePathRelativeIfInsideProject(picked);
        }
        EditorGUILayout.EndHorizontal();

        // Hint
        EditorGUILayout.HelpBox("You can paste an absolute path or a path relative to the project root.", MessageType.Info);

        EditorGUILayout.Space(6);
        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(inputJsonPath)))
        {
            if (GUILayout.Button("Export Floorplan + Furniture PNGs", GUILayout.Height(32)))
            {
                try
                {
                    DoExport(inputJsonPath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Debug_JsonRebuilder] Export failed: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }

    //--------------------------------------------------------------------
    // Core
    //--------------------------------------------------------------------
    private static void DoExport(string userPath)
    {
        string absJsonPath = ResolveToAbsolute(userPath);
        if (!File.Exists(absJsonPath))
            throw new FileNotFoundException("JSON file not found", absJsonPath);

        string json = File.ReadAllText(absJsonPath, Encoding.UTF8);

        // Output next to JSON
        string dir = Path.GetDirectoryName(absJsonPath) ?? GetProjectRoot();
        string baseName = Path.GetFileNameWithoutExtension(absJsonPath);
        string floorplanOut = Path.Combine(dir, baseName + "_floorplan.png");
        string furnitureOut = Path.Combine(dir, baseName + "_furniture.png");

        // 1) Floorplan: decode base64 directly
        WriteFloorplanFromJson(json, floorplanOut);

        // 2) Furniture: call into SessionDataExporter.RenderAndExportFurniture(...)
        WriteFurnitureViaExporter(json, furnitureOut);

        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(dir);
        Debug.Log($"[Debug_JsonRebuilder] ✅ Exported:\n  {floorplanOut}\n  {furnitureOut}");
    }

    private static void WriteFloorplanFromJson(string json, string outPngPath)
    {
        {
            var min = JsonUtility.FromJson<ExportSessionData_Min>(json);
            if (min == null || min.floorplan == null || string.IsNullOrEmpty(min.floorplan.base64))
                throw new InvalidOperationException("No floorplan.base64 found in session JSON.");

#if UNITY_EDITOR
            Debug.Log($"[Debug_JsonRebuilder] Rebuilding floorplan with uvScale={min.floorplan.uvScale}, uvOffset={min.floorplan.uvOffset}, src={min.floorplan.imageDimensions.x}x{min.floorplan.imageDimensions.y}");
#endif

            // Decode the source PNG from base64
            byte[] srcBytes = Convert.FromBase64String(min.floorplan.base64);
            var srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!srcTex.LoadImage(srcBytes))
                throw new InvalidOperationException("Failed to decode floorplan PNG from base64.");

            // Output is 1024×1024 (match exporter outputResolution); adjust if you expose it
            int outSize = 1024;
            var outTex = new Texture2D(outSize, outSize, TextureFormat.RGBA32, false, false);

            // Pull UV transform; default to identity if missing
            Vector2 uvScale = (min.floorplan.uvScale == Vector2.zero) ? Vector2.one : min.floorplan.uvScale;
            Vector2 uvOffset = min.floorplan.uvOffset; // (0,0) default is fine

            Color32 transparent = new Color32(0, 0, 0, 0);
            var pixels = new Color32[outSize * outSize];

            for (int y = 0; y < outSize; y++)
            {
                float v = (y + 0.5f) / outSize;   // output UV.y in [0,1]
                for (int x = 0; x < outSize; x++)
                {
                    float u = (x + 0.5f) / outSize; // output UV.x in [0,1]

                    // Apply material UV transform from JSON
                    float su = u * uvScale.x + uvOffset.x;
                    float sv = v * uvScale.y + uvOffset.y;

                    // Outside source range → transparent letterbox pixel
                    if (su < 0f || su > 1f || sv < 0f || sv > 1f)
                    {
                        pixels[y * outSize + x] = transparent;
                        continue;
                    }

                    // Sample source using nearest-neighbor (fast/stable for maps)
                    int sx = Mathf.Clamp(Mathf.FloorToInt(su * srcTex.width), 0, srcTex.width - 1);
                    int sy = Mathf.Clamp(Mathf.FloorToInt(sv * srcTex.height), 0, srcTex.height - 1);
                    pixels[y * outSize + x] = srcTex.GetPixel(sx, sy);
                }
            }

            outTex.SetPixels32(pixels);
            outTex.Apply();

            byte[] png = outTex.EncodeToPNG();
            File.WriteAllBytes(outPngPath, png);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(srcTex);
            UnityEngine.Object.DestroyImmediate(outTex);
        }
    }

    private static void WriteFurnitureViaExporter(string json, string outPngPath)
    {
        // Locate the exporter type
        var exporterType = FindTypeAnywhere("SessionDataExporter");
        if (exporterType == null)
            throw new MissingMemberException("Could not find type 'SessionDataExporter' in loaded assemblies.");

        // Find the private (or public) method: void RenderAndExportFurniture(ExportSessionData, string)
        var mi = exporterType.GetMethod("RenderAndExportFurniture",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (mi == null)
            throw new MissingMethodException("Method 'RenderAndExportFurniture' not found on SessionDataExporter.");

        // Find the exporter’s ExportSessionData type
        var exporterSessionType = FindTypeAnywhere("ExportSessionData");
        if (exporterSessionType == null)
            throw new MissingMemberException("Could not find 'ExportSessionData' type (used by SessionDataExporter).");

        // Round-trip the JSON into the exporter’s exact session type
        object exporterSessionObj = JsonUtility.FromJson(json, exporterSessionType);

        // Create a temporary GO to host the component (if SessionDataExporter is a MonoBehaviour)
        UnityEngine.Object tempObj = null;
        try
        {
            Component exporterInstance = null;
            if (typeof(MonoBehaviour).IsAssignableFrom(exporterType))
            {
                var go = new GameObject("__ExporterTemp");
                tempObj = go;
                exporterInstance = (Component)go.AddComponent(exporterType);
            }
            else
            {
                // If it's not a MonoBehaviour, instantiate directly
                exporterInstance = (Component)Activator.CreateInstance(exporterType);
            }

            // Ensure target folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(outPngPath) ?? GetProjectRoot());

            // Invoke exporter logic
            mi.Invoke(exporterInstance, new object[] { exporterSessionObj, outPngPath });
        }
        finally
        {
            if (tempObj != null)
                UnityEngine.Object.DestroyImmediate(tempObj);
        }
    }

    //--------------------------------------------------------------------
    // Utilities
    //--------------------------------------------------------------------
    private static Type FindTypeAnywhere(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return t;
        }
        return null;
    }

    private static string ResolveToAbsolute(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;

        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        // Treat as project-root relative
        return Path.GetFullPath(Path.Combine(GetProjectRoot(), path));
    }

    private static string GetProjectRoot()
    {
        // Application.dataPath ends with "/ProjectName/Assets"
        var dataPath = Application.dataPath;
        return Directory.GetParent(dataPath)?.FullName ?? dataPath;
    }

    private static string MakePathRelativeIfInsideProject(string absolute)
    {
        var root = GetProjectRoot();
        var abs = Path.GetFullPath(absolute);
        if (abs.StartsWith(root))
        {
            var rel = abs.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return rel;
        }
        return abs;
    }
}
#endif
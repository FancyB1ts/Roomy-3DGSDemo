// Assets/01.Scripts/SessionManagement/DebugOverlay.cs
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class DebugOverlay : MonoBehaviour
{
    public SessionDataExporter exporter;
    public SessionDataUploader uploader;

    static bool EnabledFromUrl()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var url = Application.absoluteURL ?? "";
        return url.Contains("debug=1");
#else
        return true; // always on in Editor for convenience
#endif
    }

    void Start()
    {
        if (!EnabledFromUrl()) { gameObject.SetActive(false); return; }

        // Minimal overlay with a single button (top-right)
        var canvas = new GameObject("DebugCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = canvas.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;

        var btn = new GameObject("DumpBtn", typeof(RectTransform), typeof(Button), typeof(Image));
        btn.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)btn.transform;
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-12, -12);
        rt.sizeDelta = new Vector2(160, 46);

        var txt = new GameObject("Txt", typeof(Text));
        txt.transform.SetParent(btn.transform, false);
        var t = txt.GetComponent<Text>();
        t.text = "Dump JSON + Stats";
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        var txrt = (RectTransform)txt.transform;
        txrt.anchorMin = Vector2.zero;
        txrt.anchorMax = Vector2.one;
        txrt.offsetMin = Vector2.zero;
        txrt.offsetMax = Vector2.zero;

        btn.GetComponent<Button>().onClick.AddListener(Dump);
    }

    void Dump()
    {
        if (exporter == null)
        {
            Debug.LogError("[DBG] Exporter reference is NULL. Wire the scene instance.");
            return;
        }

        string json = exporter.GetCurrentSessionAsJson();

        // Basic info
        Debug.Log($"[DBG] Exporter={exporter.name} scene={exporter.gameObject.scene.name}");
        Debug.Log($"[DBG] JSON length: {json?.Length ?? 0}");
        if (!string.IsNullOrEmpty(json))
        {
            var head = json.Length > 800 ? json.Substring(0, 800) : json;
            Debug.Log("[DBG] JSON head:\n" + head);
        }

        // Heuristic field probes (no schema dependency on your C# types)
        int furnitureCount = TryCountFurniture(json);
        int floorplanBase64Len = TryGetFloorplanBase64Length(json);
        float scaleValue = TryGetScaleValue(json);

        Debug.Log($"[DBG] furnitureCount={furnitureCount}  floorplanBase64Len={floorplanBase64Len}  scaleValue={scaleValue}");

        // Persist last dump locally (handy after refresh)
        PlayerPrefs.SetString("last_json_dump", json ?? "");
        PlayerPrefs.Save();

        // Optional: echo through uploader (does not send to server)
        if (uploader != null)
        {
            // Comment out the next line if you didn't add DebugEcho() to the uploader.
            uploader.DebugEcho(json);
        }
    }

    // --- Helpers: robust-ish regex probes for quick diagnostics ---

    // Counts number of top-level objects inside "furniture": [ ... ] by counting '{' occurrences in the captured array block.

    public static int TryCountFurniture(string json)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        try
        {
            var m = Regex.Match(json, "\"furniture\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
            if (!m.Success) return 0;
            var block = m.Groups[1].Value;
            // Count "{" occurrences as a fast proxy for number of objects
            int count = 0;
            foreach (Match obj in Regex.Matches(block, "\\{"))
                count++;
            return count;
        }
        catch { return 0; }
    }

    // Extracts: "floorplan": { ... "base64":"<data>" ... }
    public static int TryGetFloorplanBase64Length(string json)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        try
        {
            // First capture the floorplan object
            var fp = Regex.Match(json, "\"floorplan\"\\s*:\\s*\\{(.*?)\\}", RegexOptions.Singleline);
            if (!fp.Success) return 0;
            var block = fp.Groups[1].Value;

            var b64 = Regex.Match(block, "\"base64\"\\s*:\\s*\"(.*?)\"", RegexOptions.Singleline);
            if (!b64.Success) return 0;
            return b64.Groups[1].Value.Length;
        }
        catch { return 0; }
    }

    // Extracts: "scaleValue": <number> (either inside floorplan or top-level; tries both)
    public static float TryGetScaleValue(string json)
    {
        if (string.IsNullOrEmpty(json)) return 0f;
        try
        {
            var m = Regex.Match(json, "\"scaleValue\"\\s*:\\s*([0-9]+\\.?[0-9]*)");
            if (m.Success && float.TryParse(m.Groups[1].Value, out var f)) return f;

            // Try inside floorplan object explicitly
            var fp = Regex.Match(json, "\"floorplan\"\\s*:\\s*\\{(.*?)\\}", RegexOptions.Singleline);
            if (fp.Success)
            {
                var block = fp.Groups[1].Value;
                var m2 = Regex.Match(block, "\"scaleValue\"\\s*:\\s*([0-9]+\\.?[0-9]*)");
                if (m2.Success && float.TryParse(m2.Groups[1].Value, out var f2)) return f2;
            }
        }
        catch { /* ignore */ }
        return 0f;
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Slicer;
using System.IO;

[System.Serializable]
public class ExportSessionData
{
    public string sessionId;
    public string userId;
    public string timestamp;
    public ExportFloorplanData floorplan;
    public List<ExportFurnitureData> furniture = new List<ExportFurnitureData>();
}

[System.Serializable]
public class ExportFloorplanData
{
    public string base64;
    public float scaleValue;
    public Vector2Int imageDimensions;
    public Vector2 uvScale;
    public Vector2 uvOffset;
}

[System.Serializable]
public class ExportFurnitureData
{
    public string furnitureType;
    public int itemDataId;
    public ExportPositionData position;
    public float rotation;
    public ExportDimensionData dimensions;
    public Vector2 forwardDirection;
}

[System.Serializable]
public class ExportPositionData
{
    public Vector3 world;
    public Vector2 normalized;
}

[System.Serializable]
public class ExportDimensionData
{
    public Vector3 original;
    public Vector3 current;
}

public class SessionDataExporter : MonoBehaviour
{
    [Header("Scene References")]
    public Transform floorplanPlane;

    [Header("Export Settings")]
    public int outputResolution = 1024;
    public string exportPath = "SessionData/";

    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool openFolderAfterExport = true;

    // Furniture color mapping for consistent ML training data
    private Dictionary<string, Color32> furnitureColors = new Dictionary<string, Color32>()
    {
        {"PF_F_ApplianceHigh", new Color32(255, 0, 0, 255)},      // Red
        {"PF_F_ApplianceLow", new Color32(255, 128, 0, 255)},     // Orange  
        {"PF_F_Armchair", new Color32(255, 255, 0, 255)},         // Yellow
        {"PF_F_BedBig", new Color32(128, 255, 0, 255)},           // Lime
        {"PF_F_BedsideTable", new Color32(0, 255, 0, 255)},       // Green
        {"PF_F_BedSmall", new Color32(0, 255, 128, 255)},         // Spring Green
        {"PF_F_Chair", new Color32(0, 255, 255, 255)},            // Cyan
        {"PF_F_Closet", new Color32(0, 128, 255, 255)},           // Azure
        {"PF_F_Desk", new Color32(0, 0, 255, 255)},               // Blue
        {"PF_F_DiningTable", new Color32(128, 0, 255, 255)},      // Violet
        {"PF_F_LowTable", new Color32(255, 0, 255, 255)},         // Magenta
        {"PF_F_OfficeChair", new Color32(255, 0, 128, 255)},      // Rose
        {"PF_F_Shelf", new Color32(128, 255, 128, 255)},          // Light Green
        {"PF_F_Sofa", new Color32(255, 128, 128, 255)},           // Light Red
        {"PF_F_Storage", new Color32(128, 128, 255, 255)},        // Light Blue
        {"PF_F_TVSet", new Color32(255, 128, 64, 255)},           // Light Orange
        {"FALLBACK", new Color32(0, 0, 0, 255)}                   // Black fallback
    };

    private string currentSessionId;

    void Start()
    {
        currentSessionId = GenerateSessionId();
        DetectReferences();
    }

    /// <summary>
    /// Main export function - call this from UI button
    /// </summary>
    public void ExportCurrentSession()
    {
        try
        {
            Log("[SessionDataExporter] Starting session export...");

            // Collect session data
            ExportSessionData sessionData = CollectSessionData();

            // Generate file names
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string baseFileName = $"{timestamp}_session_{currentSessionId}";
            string fullExportPath = Path.Combine(Application.persistentDataPath, exportPath);
            Directory.CreateDirectory(fullExportPath);

            // Export JSON
            string jsonPath = Path.Combine(fullExportPath, $"{baseFileName}_data.json");
            ExportJsonData(sessionData, jsonPath);

            // Render and export PNGs
            string floorplanPath = Path.Combine(fullExportPath, $"{baseFileName}_floorplan.png");
            string furniturePath = Path.Combine(fullExportPath, $"{baseFileName}_furniture.png");

            RenderAndExportFloorplan(floorplanPath);
            RenderAndExportFurniture(sessionData, furniturePath);

            Log($"[SessionDataExporter] ✅ Export completed!");
            Log($"Files: {baseFileName}_data.json, {baseFileName}_floorplan.png, {baseFileName}_furniture.png");

            if (openFolderAfterExport && Application.isPlaying)
            {
                OpenExportFolder(fullExportPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionDataExporter] Export failed: {e.Message}\n{e.StackTrace}");
        }
    }

    private ExportSessionData CollectSessionData()
    {
        return new ExportSessionData
        {
            sessionId = currentSessionId,
            userId = "anonymous",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            floorplan = CollectFloorplanData(),
            furniture = CollectFurnitureData()
        };
    }

    private ExportFloorplanData CollectFloorplanData()
    {
        ExportFloorplanData data = new ExportFloorplanData();

        // Get scale value
        if (floorplanPlane != null)
        {
            data.scaleValue = floorplanPlane.localScale.x;
            Log($"[SessionDataExporter] Floorplan scale: {data.scaleValue}");
        }

        // Get floorplan material and UV values
        if (floorplanPlane != null)
        {
            Renderer planeRenderer = floorplanPlane.GetComponent<Renderer>();
            if (planeRenderer != null && planeRenderer.material != null)
            {
                Material floorplanMaterial = planeRenderer.material;

                // Get UV transform values that shader calculated
                data.uvScale = floorplanMaterial.mainTextureScale;
                data.uvOffset = floorplanMaterial.mainTextureOffset;

                Log($"[SessionDataExporter] UV Scale: {data.uvScale}, Offset: {data.uvOffset}");
            }
        }

        // Create square floorplan image with letterboxing
        Texture2D squareFloorplan = CreateSquareFloorplan();
        if (squareFloorplan != null)
        {
            byte[] pngBytes = squareFloorplan.EncodeToPNG();
            data.base64 = Convert.ToBase64String(pngBytes);
            data.imageDimensions = new Vector2Int(squareFloorplan.width, squareFloorplan.height);

            Log($"[SessionDataExporter] Created square floorplan: {squareFloorplan.width}x{squareFloorplan.height}");
            DestroyImmediate(squareFloorplan);
        }

        return data;
    }

    private List<ExportFurnitureData> CollectFurnitureData()
    {
        List<ExportFurnitureData> furnitureList = new List<ExportFurnitureData>();

        // Find all furniture objects using top-down search
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("(Clone)"))
            {
                // Search for FurnitureUIController in ROOT path
                Transform root = obj.transform.Find("ROOT");
                if (root != null)
                {
                    var controller = root.GetComponentInChildren<FurnitureUIController>();
                    if (controller != null)
                    {
                        ExportFurnitureData furniture = CreateFurnitureData(obj);
                        if (furniture != null)
                        {
                            furnitureList.Add(furniture);
                        }
                    }
                }
            }
        }

        Log($"[SessionDataExporter] Found {furnitureList.Count} furniture pieces");
        return furnitureList;
    }

    private ExportFurnitureData CreateFurnitureData(GameObject furnitureObj)
    {
        try
        {
            ExportFurnitureData furniture = new ExportFurnitureData();

            // Get furniture type from name
            furniture.furnitureType = GetFurnitureType(furnitureObj);
            furniture.itemDataId = GetItemDataId(furniture.furnitureType);

            // Get position data with debug
            Vector3 worldPos = furnitureObj.transform.position;
            Vector2 normalized = WorldToNormalized(worldPos);

            Log($"[Debug] Furniture: {furnitureObj.name}");
            Log($"[Debug]   World pos: {worldPos}");
            Log($"[Debug]   Plane center: {floorplanPlane.position}");
            Log($"[Debug]   Plane scale: {floorplanPlane.localScale}");
            Log($"[Debug]   Calculated normalized: {normalized}");

            furniture.position = new ExportPositionData
            {
                world = worldPos,
                normalized = normalized
            };

            // Get rotation and forward direction
            furniture.rotation = furnitureObj.transform.eulerAngles.y;
            Vector3 forward = furnitureObj.transform.forward;
            furniture.forwardDirection = new Vector2(forward.x, forward.z);

            // Get dimensions
            furniture.dimensions = GetFurnitureDimensions(furnitureObj);

            return furniture;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionDataExporter] Failed to create furniture data for {furnitureObj.name}: {e.Message}");
            return null;
        }
    }

    private string GetFurnitureType(GameObject furnitureObj)
    {
        string furnitureType = furnitureObj.name.Replace("(Clone)", "").Trim();
        return furnitureColors.ContainsKey(furnitureType) ? furnitureType : "FALLBACK";
    }

    private int GetItemDataId(string furnitureType)
    {
        return Mathf.Abs(furnitureType.GetHashCode()) % 1000;
    }

    private Vector2 WorldToNormalized(Vector3 worldPos)
    {
        if (floorplanPlane == null) return Vector2.zero;

        // Convert world position to normalized coordinates (0-1) relative to floorplan plane
        Vector3 planeCenter = floorplanPlane.position;
        Vector3 planeScale = floorplanPlane.localScale;
        float planeHalfSizeX = 5f * planeScale.x; // Unity plane is 10x10 at scale 1
        float planeHalfSizeZ = 5f * planeScale.z;

        float normalizedX = (worldPos.x - planeCenter.x + planeHalfSizeX) / (planeHalfSizeX * 2f);
        float normalizedZ = (worldPos.z - planeCenter.z + planeHalfSizeZ) / (planeHalfSizeZ * 2f);

        return new Vector2(Mathf.Clamp01(normalizedX), Mathf.Clamp01(normalizedZ));
    }

    private ExportDimensionData GetFurnitureDimensions(GameObject furnitureObj)
    {
        ExportDimensionData dimensions = new ExportDimensionData();

        // Get current dimensions from collider instead of mesh bounds
        dimensions.current = GetColliderSize(furnitureObj);

        // Calculate original dimensions from SlicerController on root object
        var slicerController = furnitureObj.GetComponent<SlicerController>();
        if (slicerController != null)
        {
            Vector3 slicerSize = slicerController.Size;
            dimensions.original = new Vector3(
                dimensions.current.x / slicerSize.x,
                dimensions.current.y / slicerSize.y,
                dimensions.current.z / slicerSize.z
            );
        }
        else
        {
            dimensions.original = dimensions.current;
        }

        return dimensions;
    }

    private Texture2D CreateSquareFloorplan()
    {
        if (floorplanPlane == null)
        {
            LogError("[SessionDataExporter] Floorplan plane not assigned");
            return null;
        }

        // Get the floorplan material
        Renderer planeRenderer = floorplanPlane.GetComponent<Renderer>();
        if (planeRenderer == null || planeRenderer.material == null)
        {
            LogError("[SessionDataExporter] No material found on floorplan plane");
            return null;
        }

        Material floorplanMaterial = planeRenderer.material;
        Texture2D originalTexture = floorplanMaterial.mainTexture as Texture2D;

        if (originalTexture == null)
        {
            LogError("[SessionDataExporter] No texture found in floorplan material");
            return null;
        }

        // Get UV transform values that shader calculated
        Vector2 uvScale = floorplanMaterial.mainTextureScale;
        Vector2 uvOffset = floorplanMaterial.mainTextureOffset;

        Log($"[SessionDataExporter] Original texture: {originalTexture.width}x{originalTexture.height}");
        Log($"[SessionDataExporter] UV Scale: {uvScale}, Offset: {uvOffset}");

        // Create square output texture
        Texture2D squareTexture = new Texture2D(outputResolution, outputResolution, TextureFormat.RGB24, false);
        Color[] pixels = new Color[outputResolution * outputResolution];

        // Get base color from material for letterbox areas
        Color baseColor = Color.white;
        if (floorplanMaterial.HasProperty("_BaseColor"))
        {
            baseColor = floorplanMaterial.GetColor("_BaseColor");
        }

        // Apply UV transformation (same logic as shader)
        for (int y = 0; y < outputResolution; y++)
        {
            for (int x = 0; x < outputResolution; x++)
            {
                // Convert pixel coordinates to UV (0-1)
                float u = (float)x / outputResolution;
                float v = (float)y / outputResolution;

                // Apply shader's UV transformation: UV_final = UV_original * scale + offset
                Vector2 transformedUV = new Vector2(
                    u * uvScale.x + uvOffset.x,
                    v * uvScale.y + uvOffset.y
                );

                Color pixelColor;

                // Check if UV is within texture bounds (same as shader's border check)
                if (transformedUV.x >= 0f && transformedUV.x <= 1f &&
                    transformedUV.y >= 0f && transformedUV.y <= 1f)
                {
                    // Sample from original texture
                    pixelColor = originalTexture.GetPixelBilinear(transformedUV.x, transformedUV.y);
                }
                else
                {
                    // Outside bounds - use base color (letterbox area)
                    pixelColor = baseColor;
                }

                pixels[y * outputResolution + x] = pixelColor;
            }
        }

        squareTexture.SetPixels(pixels);
        squareTexture.Apply();

        return squareTexture;
    }

    private void RenderAndExportFloorplan(string filePath)
    {
        Texture2D floorplanTexture = CreateSquareFloorplan();
        if (floorplanTexture != null)
        {
            byte[] pngBytes = floorplanTexture.EncodeToPNG();
            File.WriteAllBytes(filePath, pngBytes);

            Log($"[SessionDataExporter] Floorplan PNG exported: {filePath}");
            DestroyImmediate(floorplanTexture);
        }
    }

    private void RenderAndExportFurniture(ExportSessionData sessionData, string filePath)
    {
        // Create transparent texture for furniture overlay
        Texture2D furnitureTexture = new Texture2D(outputResolution, outputResolution, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[outputResolution * outputResolution];

        // Fill with transparent
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(0, 0, 0, 0);
        }

        // Draw furniture rectangles
        foreach (var furniture in sessionData.furniture)
        {
            float scaleHint = (sessionData != null && sessionData.floorplan != null && sessionData.floorplan.scaleValue > 0f)
                ? sessionData.floorplan.scaleValue : 0f;
            DrawFurnitureRectangle(pixels, furniture, scaleHint);
        }

        furnitureTexture.SetPixels32(pixels);
        furnitureTexture.Apply();

        // Export
        byte[] pngBytes = furnitureTexture.EncodeToPNG();
        File.WriteAllBytes(filePath, pngBytes);

        Log($"[SessionDataExporter] Furniture PNG exported: {filePath}");
        DestroyImmediate(furnitureTexture);
    }

    private void DrawFurnitureRectangle(Color32[] pixels, ExportFurnitureData furniture, float planeScaleHint)
    {
        // Get furniture color
        Color32 color = furnitureColors.ContainsKey(furniture.furnitureType)
            ? furnitureColors[furniture.furnitureType]
            : furnitureColors["FALLBACK"];

        // Convert normalized position to pixel coordinates
        int centerX = Mathf.RoundToInt(furniture.position.normalized.x * outputResolution);
        int centerY = Mathf.RoundToInt(furniture.position.normalized.y * outputResolution);

        // Optional debug log for scale hint usage
        if (enableDebugLogs && floorplanPlane == null && planeScaleHint > 0f)
        {
            Log($"[PNG] Using JSON scale hint {planeScaleHint:F3} for size calculation.");
        }

        // Calculate furniture size in pixels using stored collider dimensions
        Vector2 normalizedSize = CalculateNormalizedSize(furniture.dimensions.current, planeScaleHint);
        float pixelWidth = normalizedSize.x * outputResolution;
        float pixelHeight = normalizedSize.y * outputResolution;

        // Get rotation in radians
        float rotationRadians = -furniture.rotation * Mathf.Deg2Rad;

        // Draw rotated rectangle
        DrawRotatedRectangle(pixels, centerX, centerY, pixelWidth, pixelHeight, rotationRadians, color, furniture.furnitureType);
    }

    private void DrawRotatedRectangle(Color32[] pixels, int centerX, int centerY, float width, float height, float rotationRadians, Color32 color, string furnitureType)
    {
        // Calculate half dimensions
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;

        // Pre-calculate rotation values
        float cosAngle = Mathf.Cos(rotationRadians);
        float sinAngle = Mathf.Sin(rotationRadians);

        // Calculate the bounding box of the rotated rectangle to limit our iteration
        float maxExtent = Mathf.Max(halfWidth, halfHeight) * 1.5f; // Some padding for safety
        int minX = Mathf.Max(0, centerX - Mathf.CeilToInt(maxExtent));
        int maxX = Mathf.Min(outputResolution - 1, centerX + Mathf.CeilToInt(maxExtent));
        int minY = Mathf.Max(0, centerY - Mathf.CeilToInt(maxExtent));
        int maxY = Mathf.Min(outputResolution - 1, centerY + Mathf.CeilToInt(maxExtent));

        Log($"[PNG] Drawing rotated {furnitureType} at pixel ({centerX}, {centerY}) " +
                  $"size ({width:F1}, {height:F1}) rotation {rotationRadians * Mathf.Rad2Deg:F1}°");

        // Iterate through the bounding area
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                // Translate to rectangle's local coordinate system (center at origin)
                float localX = x - centerX;
                float localY = y - centerY;

                // Rotate the point to align with rectangle's local axes
                float rotatedX = localX * cosAngle + localY * sinAngle;
                float rotatedY = -localX * sinAngle + localY * cosAngle;

                // Check if the rotated point is inside the rectangle
                if (Mathf.Abs(rotatedX) <= halfWidth && Mathf.Abs(rotatedY) <= halfHeight)
                {
                    int index = y * outputResolution + x;
                    pixels[index] = color;
                }
            }
        }
    }

    private Vector2 CalculateNormalizedSize(Vector3 worldSize, float planeScaleHint)
    {
        // If we have the floorplan plane in the scene, use it (original behavior)
        if (floorplanPlane != null)
        {
            Vector3 planeScale = floorplanPlane.localScale;
            float planeWorldSizeX = 10f * planeScale.x; // Unity plane is 10x10 at scale 1
            float planeWorldSizeZ = 10f * planeScale.z;

            return new Vector2(
                planeWorldSizeX > 0f ? worldSize.x / planeWorldSizeX : 0f,
                planeWorldSizeZ > 0f ? worldSize.z / planeWorldSizeZ : 0f
            );
        }

        // Fallback for JSON-only reconstruction (no scene). Use scale hint from session JSON.
        if (planeScaleHint > 0f)
        {
            float planeWorldSize = 10f * planeScaleHint; // assume uniform scale used during export
            return new Vector2(
                planeWorldSize > 0f ? worldSize.x / planeWorldSize : 0f,
                planeWorldSize > 0f ? worldSize.z / planeWorldSize : 0f
            );
        }

        // No reference available
        LogWarning("[SessionDataExporter] CalculateNormalizedSize: No floorplan plane and no scale hint; returning zero size.");

        return Vector2.zero;
    }

    private Vector3 GetColliderSize(GameObject furnitureObj)
    {
        // Look for BoxCollider on the furniture object or its children
        BoxCollider boxCollider = furnitureObj.GetComponentInChildren<BoxCollider>();

        if (boxCollider != null)
        {
            // Get the actual world-space size of the collider
            Vector3 colliderSize = boxCollider.size;
            Vector3 scale = boxCollider.transform.lossyScale;

            // Apply the transform scale to get actual world dimensions
            Vector3 worldColliderSize = Vector3.Scale(colliderSize, scale);

            Log($"[Collider] {furnitureObj.name}: Local={colliderSize}, Scale={scale}, World={worldColliderSize}");

            return worldColliderSize;
        }

        // Fallback to mesh bounds if no collider found
        LogWarning($"[Collider] No BoxCollider found on {furnitureObj.name}, falling back to mesh bounds");

        Transform root = furnitureObj.transform.Find("ROOT");
        if (root != null)
        {
            var meshRenderer = root.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                return meshRenderer.bounds.size;
            }
        }

        return Vector3.one; // Ultimate fallback
    }

    private void DetectReferences()
    {
        if (floorplanPlane == null)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("floorplan") || obj.name.ToLower().Contains("ground"))
                {
                    if (obj.GetComponent<Renderer>() != null)
                    {
                        floorplanPlane = obj.transform;
                        Log($"[SessionDataExporter] Auto-found floorplan plane: {floorplanPlane.name}");
                        break;
                    }
                }
            }
        }
    }

    private void ExportJsonData(ExportSessionData data, string filePath)
    {
        string jsonData = JsonUtility.ToJson(data, true);
        File.WriteAllBytes(filePath, System.Text.Encoding.UTF8.GetBytes(jsonData));
        Log($"[SessionDataExporter] JSON exported: {filePath}");
    }

    private void OpenExportFolder(string folderPath)
    {
        try
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            System.Diagnostics.Process.Start("explorer.exe", folderPath.Replace('/', '\\'));
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", folderPath);
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            System.Diagnostics.Process.Start("xdg-open", folderPath);
#endif
        }
        catch (Exception e)
        {
            LogWarning($"[SessionDataExporter] Could not open folder: {e.Message}");
        }
    }

    private string GenerateSessionId()
    {
        return "anonymous_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    public string GetCurrentSessionId()
    {
        return currentSessionId;
    }

    public void GenerateNewSessionId()
    {
        currentSessionId = GenerateSessionId();
        Log($"[SessionDataExporter] New session ID: {currentSessionId}");
    }

    [ContextMenu("Debug Furniture Search")]
    public void DebugFurnitureSearch()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int cloneCount = 0;
        int validFurnitureCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("(Clone)"))
            {
                cloneCount++;
                Log($"[Debug] Clone found: {obj.name}");

                // Check ROOT path for FurnitureUIController
                Transform root = obj.transform.Find("ROOT");
                if (root != null)
                {
                    var controller = root.GetComponentInChildren<FurnitureUIController>();
                    if (controller != null)
                    {
                        validFurnitureCount++;
                        Log($"[Debug] ✅ Valid furniture: {obj.name} at {obj.transform.position}");
                        Log($"[Debug]   - FurnitureUIController found on: {controller.gameObject.name}");

                        // Check for mesh renderer
                        var meshRenderer = root.GetComponentInChildren<MeshRenderer>();
                        Log($"[Debug]   - MeshRenderer found: {meshRenderer != null}");
                        if (meshRenderer != null)
                        {
                            Log($"[Debug]   - Mesh on: {meshRenderer.gameObject.name}");
                            Log($"[Debug]   - Bounds: {meshRenderer.bounds.size}");
                        }
                    }
                    else
                    {
                        Log($"[Debug] ❌ ROOT found but no FurnitureUIController: {obj.name}");
                    }
                }
                else
                {
                    Log($"[Debug] ❌ No ROOT path found: {obj.name}");
                }
            }
        }

        Log($"[Debug] Summary: {cloneCount} clones, {validFurnitureCount} valid furniture");
    }

    [ContextMenu("Print Export Path")]
    public void PrintExportPath()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, exportPath);
        Log($"[SessionDataExporter] Export path: {fullPath}");
    }

    public string GetCurrentSessionAsJson()
    {
        ExportSessionData sessionData = CollectSessionData();
        return JsonUtility.ToJson(sessionData, true); // Pretty-printed JSON
    }

    [ContextMenu("Debug Coordinate Systems")]
    public void DebugCoordinateSystems()
    {
        if (floorplanPlane == null) return;

        Log("=== COORDINATE SYSTEM DEBUG ===");
        Log($"Floorplan center: {floorplanPlane.position}");
        Log($"Floorplan scale: {floorplanPlane.localScale}");

        // Test a few known world positions
        Vector3[] testPositions = {
        floorplanPlane.position + Vector3.right * 2f,  // Right of center
        floorplanPlane.position + Vector3.left * 2f,   // Left of center  
        floorplanPlane.position + Vector3.forward * 2f, // Forward from center
        floorplanPlane.position + Vector3.back * 2f     // Back from center
    };

        for (int i = 0; i < testPositions.Length; i++)
        {
            Vector3 worldPos = testPositions[i];
            Vector2 normalized = WorldToNormalized(worldPos);
            Log($"Test {i}: World {worldPos} → Normalized {normalized}");
        }
    }

    private void Log(string message)
    {
        if (enableDebugLogs) Debug.Log(message);
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs) Debug.LogWarning(message);
    }

    private void LogError(string message)
    {
        if (enableDebugLogs) Debug.LogError(message);
    }

}
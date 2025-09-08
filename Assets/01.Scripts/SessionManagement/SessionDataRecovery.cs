using System;
using System.IO;
using UnityEngine;

public class SessionDataRecovery : MonoBehaviour
{
    [Header("Scene References")]
    public Transform floorplanPlane;
    public SessionDataExporter sessionDataExporter;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    [Header("Recovery Settings")]
    public string exportPath = "SessionData/";

    void Start()
    {
        DetectReferences();
    }

    /// <summary>
    /// UI Button click function for loading session
    /// </summary>
    public void LoadSessionButtonClick()
    {
        // For now, load the most recent session file
        string exportDirectory = Path.Combine(Application.persistentDataPath, exportPath);
        
        if (!Directory.Exists(exportDirectory))
        {
            Debug.LogWarning("[SessionDataRecovery] No session files found - export directory doesn't exist");
            return;
        }

        // Find most recent JSON file
        string[] jsonFiles = Directory.GetFiles(exportDirectory, "*_data.json");
        if (jsonFiles.Length == 0)
        {
            Debug.LogWarning("[SessionDataRecovery] No session files found");
            return;
        }

        // Get most recent file
        string mostRecentFile = jsonFiles[jsonFiles.Length - 1];
        
        if (enableDebugLogs) Debug.Log($"[SessionDataRecovery] Loading session: {Path.GetFileName(mostRecentFile)}");
        
        // Load and restore session
        ExportSessionData sessionData = LoadSessionData(mostRecentFile);
        if (sessionData != null)
        {
            RestoreCompleteSession(sessionData);
        }
    }

    /// <summary>
    /// Load session data from JSON file
    /// </summary>
    public ExportSessionData LoadSessionData(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                ExportSessionData sessionData = JsonUtility.FromJson<ExportSessionData>(jsonData);
                
                if (enableDebugLogs) Debug.Log($"[SessionDataRecovery] Session loaded: {sessionData.furniture.Count} furniture pieces");
                return sessionData;
            }
            else
            {
                Debug.LogError($"[SessionDataRecovery] File not found: {filePath}");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionDataRecovery] Failed to load session: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Restore complete session (floorplan + furniture)
    /// </summary>
    public void RestoreCompleteSession(ExportSessionData sessionData)
    {
        // 1. Restore floorplan first
        RestoreFloorplan(sessionData.floorplan);
        
        // 2. Restore furniture
        var placementManager = FindObjectOfType<MousePlacementManager>();
        if (placementManager != null)
        {
    //        placementManager.RestoreSession(sessionData);
        }
        else
        {
            Debug.LogError("[SessionDataRecovery] MousePlacementManager not found - cannot restore furniture");
        }
    }

    /// <summary>
    /// Restore floorplan from session data
    /// </summary>
    public void RestoreFloorplan(ExportFloorplanData floorplanData)
    {
        try
        {
            if (floorplanPlane == null)
            {
                Debug.LogError("[SessionDataRecovery] Cannot restore floorplan - plane not assigned");
                return;
            }

            // Convert base64 to texture
            Texture2D floorplanTexture = Base64ToTexture(floorplanData.base64);
            if (floorplanTexture == null) return;

            // Get floorplan material
            Renderer planeRenderer = floorplanPlane.GetComponent<Renderer>();
            if (planeRenderer == null || planeRenderer.material == null)
            {
                Debug.LogError("[SessionDataRecovery] No material found on floorplan plane");
                return;
            }

            // Apply texture and UV settings
            Material floorplanMaterial = planeRenderer.material;
            floorplanMaterial.mainTexture = floorplanTexture;
            floorplanMaterial.mainTextureScale = floorplanData.uvScale;
            floorplanMaterial.mainTextureOffset = floorplanData.uvOffset;

            // Restore scale
            Vector3 restoredScale = new Vector3(floorplanData.scaleValue, 1f, floorplanData.scaleValue);
            floorplanPlane.localScale = restoredScale;

            if (enableDebugLogs) 
            {
                Debug.Log($"[SessionDataRecovery] Floorplan restored - Scale: {floorplanData.scaleValue}, UV: {floorplanData.uvScale}, {floorplanData.uvOffset}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionDataRecovery] Failed to restore floorplan: {e.Message}");
        }
    }

    /// <summary>
    /// Convert base64 string to Texture2D
    /// </summary>
    private Texture2D Base64ToTexture(string base64Data)
    {
        try
        {
            // Remove data URL prefix if present
            string imageData = base64Data;
            if (base64Data.Contains(","))
            {
                string[] parts = base64Data.Split(',');
                if (parts.Length >= 2)
                {
                    imageData = parts[1];
                }
            }

            // Convert base64 to bytes
            byte[] imageBytes = Convert.FromBase64String(imageData);
            
            // Create texture
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            
            if (texture.LoadImage(imageBytes))
            {
                return texture;
            }
            else
            {
                Debug.LogError("[SessionDataRecovery] Failed to load image from base64 data");
                DestroyImmediate(texture);
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionDataRecovery] Error converting base64 to texture: {e.Message}");
            return null;
        }
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
                        if (enableDebugLogs) Debug.Log($"[SessionDataRecovery] Auto-found floorplan plane: {floorplanPlane.name}");
                        break;
                    }
                }
            }
        }

        if (sessionDataExporter == null)
        {
            sessionDataExporter = FindObjectOfType<SessionDataExporter>();
            if (enableDebugLogs && sessionDataExporter != null) Debug.Log("[SessionDataRecovery] Auto-found SessionDataExporter");
        }
    }

    [ContextMenu("Load Most Recent Session")]
    public void LoadMostRecentSession()
    {
        LoadSessionButtonClick();
    }
}
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class FloorplanReceiver : MonoBehaviour
{
    [Header("Material Reference")]
    public Material floorplanMaterial;

    [Header("Renderer Reference (plane)")]
    public Renderer floorplanRenderer; // assign your plane's Renderer in the Inspector
    
    [Header("UI Reference (Optional)")]
    public RawImage rawImage;  // Drag your Raw Image here in inspector
    
    [Header("Bridge Reference")]
    public UnityWebGLBridge webGLBridge;
    
    [Header("FloorplanOutliner UI Reference")]
    public GameObject scalingUIContainer;
    
    private Texture2D currentFloorplan;
    private bool outlinerStarted = false; // prevents duplicate starts

#if UNITY_WEBGL && !UNITY_EDITOR
    void Awake()
    {
        // Add a small delay to ensure all components are fully ready
        StartCoroutine(DelayedReadySignal());
    }
    
    private IEnumerator DelayedReadySignal()
    {
        // Wait a few frames for all components to initialize
        yield return null;
        yield return null;
        yield return null;
        
        // Notify JS that this receiver is alive and ready to accept messages
        Application.ExternalEval("if (window.onReceiverReady) window.onReceiverReady();");
    }
#endif
    
    void Start()
    {
        Debug.Log("FloorplanReceiver ready to process images");
        
        // Validate references
        if (floorplanMaterial == null)
        {
            Debug.LogError("FloorplanReceiver: floorplanMaterial not assigned!");
        }
        
        if (webGLBridge == null)
        {
            Debug.LogError("FloorplanReceiver: webGLBridge not assigned!");
        }
        
        if (scalingUIContainer == null)
        {
            Debug.LogWarning("FloorplanReceiver: scalingUIContainer not assigned");
        }
    }
    
    // Aliases for web SendMessage targets; route to ProcessFloorplan
    public void ReceiveFloorplan(string base64Image)
    {
        ProcessFloorplan(base64Image);
    }

    public void ReceiveImage(string base64Image)
    {
        ProcessFloorplan(base64Image);
    }

    // Called by UnityWebGLBridge
    public void ProcessFloorplan(string base64Image)
    {
        Debug.Log("FloorplanReceiver: Processing floorplan image...");
        // Prepare Outliner for a fresh run on every new floorplan (including Replace)
        outlinerStarted = false; // allow StartOutlinerWhenStable() to run again
        StartCoroutine(ProcessFloorplanImage(base64Image));
    }
    
    private IEnumerator ProcessFloorplanImage(string base64Image)
    {
        bool success = false;
        string errorMessage = null;
        
        // Validate input
        if (string.IsNullOrEmpty(base64Image))
        {
            errorMessage = "Received empty base64 image data";
        }
        else
        {
            success = ProcessImageData(base64Image, out errorMessage);
        }
        
        // Log any errors
        if (!success && !string.IsNullOrEmpty(errorMessage))
        {
            Debug.LogError(errorMessage);
        }
        
        // Complete processing and notify
        yield return StartCoroutine(CompleteProcessing(success));
    }
    
    private bool ProcessImageData(string base64Image, out string errorMessage)
    {
        errorMessage = null;
        
        try
        {
            // Remove data URL prefix if present (data:image/png;base64,)
            string imageData = base64Image;
            if (base64Image.Contains(","))
            {
                string[] parts = base64Image.Split(',');
                if (parts.Length >= 2)
                {
                    imageData = parts[1];
                    Debug.Log($"Stripped data URL prefix, format: {parts[0]}");
                }
            }
            
            // Convert base64 to byte array
            byte[] imageBytes;
            try
            {
                imageBytes = System.Convert.FromBase64String(imageData);
                Debug.Log($"Decoded {imageBytes.Length} bytes from base64");
            }
            catch (System.Exception e)
            {
                errorMessage = $"Failed to decode base64: {e.Message}";
                return false;
            }
            
            // Clean up previous texture
            if (currentFloorplan != null)
            {
                Destroy(currentFloorplan);
                currentFloorplan = null;
            }
            
            // Create new texture
            currentFloorplan = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            
            // Set wrap mode to Clamp to prevent tiling - show base color instead
            currentFloorplan.wrapMode = TextureWrapMode.Clamp;
            
            bool loadSuccess = currentFloorplan.LoadImage(imageBytes);
            
            if (loadSuccess)
            {
                Debug.Log($"Floorplan loaded successfully: {currentFloorplan.width}x{currentFloorplan.height}");
                
                // Apply texture to material
                if (floorplanRenderer != null || floorplanMaterial != null)
                {
                    ApplyTextureToMaterial();
                    UpdateRawImage();  // Also update Raw Image
                    return true;
                }
                else
                {
                    errorMessage = "Cannot apply texture - neither floorplanRenderer nor floorplanMaterial is assigned";
                    return false;
                }
            }
            else
            {
                errorMessage = "Failed to load image from byte array - possibly corrupted data";
                return false;
            }
        }
        catch (System.Exception e)
        {
            errorMessage = $"Error processing floorplan: {e.Message}\nStack trace: {e.StackTrace}";
            return false;
        }
    }
    
    private void ApplyTextureToMaterial()
    {
        // Use the renderer’s live material instance if available; fall back to the provided material
        Material targetMat = floorplanRenderer ? floorplanRenderer.material : floorplanMaterial;
        if (targetMat == null)
        {
            Debug.LogError("[Floorplan] No material to bind to (renderer/material missing)");
            return;
        }

        // Assign the texture to the material (prefer URP _BaseMap if available)
        if (targetMat.HasProperty("_BaseMap"))
        {
            targetMat.SetTexture("_BaseMap", currentFloorplan);
        }
        else
        {
            targetMat.mainTexture = currentFloorplan;
        }
        
        // Force explicit geometry queue and log it
        targetMat.renderQueue = 2000; // Force Geometry queue explicitly
        Debug.Log($"[Floorplan] Forcing plane material queue to {targetMat.renderQueue} (shader: {targetMat.shader?.name})");
        
        // Calculate aspect ratios
        float imageAspect = (float)currentFloorplan.width / currentFloorplan.height;
        float planeAspect = 1.0f; // Square plane
        
        float uvScaleX, uvScaleY, uvOffsetX, uvOffsetY;
        
        if (imageAspect > planeAspect)
        {
            // Wide image - fit to width, letterbox top/bottom
            uvScaleX = 1.0f;                        // Use full width
            uvScaleY = imageAspect;                 // Scale UP to shrink apparent size vertically
            uvOffsetX = 0.0f;
            uvOffsetY = -(uvScaleY - 1.0f) * 0.5f;  // Center vertically with negative offset
            
            Debug.Log($"Wide image detected - fitting to width with vertical letterboxing");
        }
        else
        {
            // Tall or square image - fit to height, letterbox left/right
            uvScaleX = 1.0f / imageAspect;          // Scale UP to shrink apparent size horizontally
            uvScaleY = 1.0f;                        // Use full height
            uvOffsetX = -(uvScaleX - 1.0f) * 0.5f;  // Center horizontally with negative offset
            uvOffsetY = 0.0f;
            
            Debug.Log($"Tall/square image detected - fitting to height with horizontal letterboxing");
        }
        
        // Apply UV scaling and offset (to the chosen target material)
        targetMat.mainTextureScale = new Vector2(uvScaleX, uvScaleY);
        targetMat.mainTextureOffset = new Vector2(uvOffsetX, uvOffsetY);
        
        Debug.Log($"Applied texture: aspect {imageAspect:F2}, scale: {targetMat.mainTextureScale}, offset: {targetMat.mainTextureOffset}");
        
        // Trigger MaterialSTTransfer if present on this GameObject to mirror ST to UI
        SendMessage("TransferNow", SendMessageOptions.DontRequireReceiver);
    }
    
    private void UpdateRawImage()
    {
        // Also apply to Raw Image if assigned
        if (rawImage != null)
        {
            rawImage.texture = currentFloorplan;
            
            // Multiple refresh approaches
            Canvas.ForceUpdateCanvases();
            rawImage.SetMaterialDirty();
            rawImage.SetVerticesDirty();
            
            // Force parent canvas refresh too
            Canvas parentCanvas = rawImage.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                parentCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
                // Don't call ForceUpdateCanvases() on instance - it's already called above
            }
            
            Debug.Log("Applied material to Raw Image with aggressive UI refresh");

            // Notify FloorplanScaler (if present) that the image container is now ready
            if (scalingUIContainer != null)
            {
                var scaler = scalingUIContainer.GetComponentInChildren<Component>();
                // Look specifically for a type named "FloorplanScaler" to avoid hard reference
                if (scaler == null)
                {
                    foreach (var c in scalingUIContainer.GetComponentsInChildren<Component>(true))
                    {
                        if (c != null && c.GetType().Name == "FloorplanScaler")
                        {
                            scaler = c; break;
                        }
                    }
                }
                if (scaler != null && scaler.GetType().Name == "FloorplanScaler")
                {
                    var m = scaler.GetType().GetMethod("RecomputeImageArea", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (m != null)
                    {
                        try { m.Invoke(scaler, null); Debug.Log("[FloorplanReceiver] Notified FloorplanScaler to recompute image area"); }
                        catch { /* swallow; scaler is optional */ }
                    }
                }
            }
        }
    }
    
    private IEnumerator CompleteProcessing(bool success)
    {
        // Notify bridge of completion
        if (webGLBridge != null)
        {
            webGLBridge.OnFloorplanProcessed(success);
        }
        else
        {
            Debug.LogError("Cannot notify completion - webGLBridge is null");
        }
        
        // Wait for Canvas to be fully ready (more robust for mobile)
        yield return null; // First frame
        Canvas.ForceUpdateCanvases(); // Force immediate Canvas refresh
        yield return null; // Second frame to ensure refresh completed

        // THEN auto-start outlining process if successful
        if (success)
        {
            yield return StartCoroutine(StartOutlinerWhenStable());
        }
    }

    private IEnumerator WaitForStableScreen(int stableFrames = 6, float timeoutSeconds = 1.0f)
    {
        int ok = 0; float t = 0f;
        int w = Screen.width, h = Screen.height;
        while (ok < stableFrames && t < timeoutSeconds)
        {
            yield return null;
            t += Time.unscaledDeltaTime;
            if (Screen.width == w && Screen.height == h) ok++; else { ok = 0; w = Screen.width; h = Screen.height; }
        }
    }

    private IEnumerator StartOutlinerWhenStable()
    {
        if (outlinerStarted) yield break;
        yield return WaitForStableScreen();
        StartOutliningProcess();
        outlinerStarted = true;
    }
    
    // Temporarily activate scaling UI and start outlining
    private void StartOutliningProcess()
    {
        if (scalingUIContainer != null)
        {
            Application.ExternalEval("if (window.onOutlinerActivating) window.onOutlinerActivating();");
            Debug.Log("Activating scaling UI container...");
            scalingUIContainer.SetActive(true);
            
            // Find PolygonDrawer now that container is active
            PolygonDrawer polygonDrawer = scalingUIContainer.GetComponentInChildren<PolygonDrawer>();
            
            if (polygonDrawer != null)
            {
                Debug.Log("Auto-starting polygon outlining process...");
                polygonDrawer.StartOutlining();
                
                // Set up listener for when scaling is complete
                SetupScalingCompleteListener(polygonDrawer);
            }
            else
            {
                Debug.LogWarning("PolygonDrawer not found in scaling UI container");
                scalingUIContainer.SetActive(false); // Hide it again if no PolygonDrawer
            }
        }
        else
        {
            Debug.LogError("scalingUIContainer not assigned! Drag the scaling UI parent container to FloorplanReceiver inspector.");
        }
    }
    
    // Set up listener to hide scaling UI when complete
    private void SetupScalingCompleteListener(PolygonDrawer polygonDrawer)
    {
        // Find the FloorplanScaler to listen for completion
        FloorplanScaler scaler = polygonDrawer.GetComponent<FloorplanScaler>();
        if (scaler == null)
        {
            scaler = polygonDrawer.GetComponentInChildren<FloorplanScaler>();
        }
        
        if (scaler != null)
        {
            // Find the calculate button
            Button calculateButton = scaler.calculateButton;
            if (calculateButton != null)
            {
                // Add our listener to hide the UI after calculation
                calculateButton.onClick.AddListener(() => {
                    StartCoroutine(HideScalingUIAfterDelay(1.0f)); // Small delay to let calculation complete
                });
                Debug.Log("Set up scaling completion listener");
            }
        }
    }
    
    // Hide the scaling UI after scaling is complete
    private IEnumerator HideScalingUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (scalingUIContainer != null)
        {
            Debug.Log("Hiding scaling UI container after completion");
            scalingUIContainer.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        // Clean up texture when object is destroyed
        if (currentFloorplan != null)
        {
            Destroy(currentFloorplan);
            currentFloorplan = null;
        }
    }
}
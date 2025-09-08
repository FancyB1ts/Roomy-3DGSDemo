using UnityEngine;
using System.IO;

public class FloorplanDebugTester : MonoBehaviour
{
    [Header("References")]
    public UnityWebGLBridge webGLBridge;
    public FloorplanReceiver floorplanReceiver;
    
    [Header("Test Image")]
    [Tooltip("Drag an image file from your project here")]
    public Texture2D testImage;
    
    [Header("Debug Controls")]
    [Space(10)]
    public bool testWithBridge = true;
    [Tooltip("Test directly with FloorplanReceiver (bypasses bridge)")]
    public bool testDirectReceiver = false;
    
    [Header("Manual Base64 Input")]
    [TextArea(3, 5)]
    [Tooltip("Paste base64 string here for manual testing")]
    public string manualBase64Input;
    
    void Start()
    {
        Debug.Log("FloorplanDebugTester ready - Use inspector buttons to test");
    }
    
    [ContextMenu("Test With Project Image")]
    public void TestWithProjectImage()
    {
        if (testImage == null)
        {
            Debug.LogError("No test image assigned! Drag an image to the Test Image field.");
            return;
        }
        
        string base64 = ConvertTextureToBase64(testImage);
        if (!string.IsNullOrEmpty(base64))
        {
            Debug.Log($"Converted test image to base64: {base64.Length} characters");
            SendToReceiver(base64);
        }
    }
    
    [ContextMenu("Test With Manual Base64")]
    public void TestWithManualBase64()
    {
        if (string.IsNullOrEmpty(manualBase64Input))
        {
            Debug.LogError("No manual base64 input provided!");
            return;
        }
        
        Debug.Log($"Testing with manual base64 input: {manualBase64Input.Length} characters");
        SendToReceiver(manualBase64Input);
    }
    
    [ContextMenu("Load Image From File Path")]
    public void LoadImageFromFile()
    {
        // This method helps you test with images outside the project
        string filePath = GetTestImagePath();
        
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            byte[] imageBytes = File.ReadAllBytes(filePath);
            string base64 = System.Convert.ToBase64String(imageBytes);
            string dataUrl = "data:image/png;base64," + base64;
            
            Debug.Log($"Loaded image from {filePath}: {base64.Length} characters");
            SendToReceiver(dataUrl);
        }
        else
        {
            Debug.LogError("Could not find test image file. Update GetTestImagePath() method with your image path.");
        }
    }
    
    private void SendToReceiver(string base64Data)
    {
        if (testWithBridge && webGLBridge != null)
        {
            Debug.Log("→ Testing via UnityWebGLBridge");
            webGLBridge.ReceiveFloorplan(base64Data);
        }
        else if (testDirectReceiver && floorplanReceiver != null)
        {
            Debug.Log("→ Testing directly with FloorplanReceiver");
            floorplanReceiver.ProcessFloorplan(base64Data);
        }
        else if (floorplanReceiver != null)
        {
            Debug.Log("→ Testing directly with FloorplanReceiver (default)");
            floorplanReceiver.ProcessFloorplan(base64Data);
        }
        else
        {
            Debug.LogError("No FloorplanReceiver assigned!");
        }
    }
    
    private string ConvertTextureToBase64(Texture2D texture)
    {
        try
        {
            // Convert texture to PNG bytes
            byte[] pngBytes = texture.EncodeToPNG();
            if (pngBytes == null || pngBytes.Length == 0)
            {
                Debug.LogError("Failed to encode texture to PNG");
                return null;
            }
            
            // Convert to base64 with data URL prefix (simulating browser upload)
            string base64 = System.Convert.ToBase64String(pngBytes);
            return "data:image/png;base64," + base64;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error converting texture to base64: {e.Message}");
            return null;
        }
    }
    
    private string GetTestImagePath()
    {
        // Update this path to point to a test image on your system
        // Examples:
        // Windows: @"C:\Users\YourName\Desktop\floorplan.png"
        // Mac: "/Users/YourName/Desktop/floorplan.png"
        // Unity project: Application.dataPath + "/StreamingAssets/test_floorplan.png"
        
        return Application.dataPath + "/StreamingAssets/test_floorplan.png";
    }
    
    [ContextMenu("Print Sample Base64")]
    public void PrintSampleBase64()
    {
        // Creates a simple test texture for quick testing
        Texture2D sampleTexture = new Texture2D(100, 100);
        Color[] pixels = new Color[100 * 100];
        
        // Create a simple pattern
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % 100;
            int y = i / 100;
            pixels[i] = ((x + y) % 20 < 10) ? Color.white : Color.black;
        }
        
        sampleTexture.SetPixels(pixels);
        sampleTexture.Apply();
        
        string base64 = ConvertTextureToBase64(sampleTexture);
        Debug.Log("Sample base64 (copy this to Manual Base64 Input field):");
        Debug.Log(base64);
        
        DestroyImmediate(sampleTexture);
    }
    
    void OnValidate()
    {
        // Auto-find references if not assigned
        if (webGLBridge == null)
            webGLBridge = FindObjectOfType<UnityWebGLBridge>();
        
        if (floorplanReceiver == null)
            floorplanReceiver = FindObjectOfType<FloorplanReceiver>();
    }
}
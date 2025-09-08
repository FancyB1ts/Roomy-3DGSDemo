using UnityEngine;

public class UnityWebGLBridge : MonoBehaviour
{
    [Header("References")]
    public FloorplanReceiver floorplanReceiver;
    
    void Start()
    {
        // Ensure this GameObject is named "FloorplanManager" in the scene
        if (gameObject.name != "FloorplanManager")
        {
            Debug.LogWarning($"GameObject should be named 'FloorplanManager' but is '{gameObject.name}'");
        }
        
        // Notify React that Unity is fully loaded and ready
        NotifyReactUnityReady();
        Debug.Log("UnityWebGLBridge initialized - React communication ready");
    }
    
    // Called from React/JavaScript to send floorplan data
    // This method name must match the SendMessage call from React
    public void ReceiveFloorplan(string base64Image)
    {
        Debug.Log("UnityWebGLBridge: Received floorplan from React");
        Debug.Log($"Image data length: {base64Image.Length}");
        
        // Forward to FloorplanReceiver for processing
        if (floorplanReceiver != null)
        {
            floorplanReceiver.ProcessFloorplan(base64Image);
        }
        else
        {
            Debug.LogError("FloorplanReceiver reference not set!");
            SendMessageToReact("onFloorplanLoaded", "error");
        }
    }
    
    // NEW: Show the React floorplan uploader overlay
    public void ShowFloorplanUploader()
    {
        Debug.Log("UnityWebGLBridge: Showing floorplan uploader overlay");
        SendMessageToReact("showFloorplanUploader", "");
    }
    
    // NEW: Handle cancel from React overlay
    public void OnFloorplanUploadCancelled()
    {
        Debug.Log("UnityWebGLBridge: Floorplan upload cancelled by user");
        // No additional action needed - React will handle hiding the overlay
    }
    
    // Notify React that Unity is ready
    private void NotifyReactUnityReady()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval("if(window.onUnityReady) window.onUnityReady();");
        #endif
        
        Debug.Log("Notified React: Unity is ready");
    }
    
    // Send messages back to React
    public void SendMessageToReact(string functionName, string message)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval($"if(window.{functionName}) window.{functionName}('{message}');");
        #endif
        
        Debug.Log($"Sent to React: {functionName}({message})");
    }
    
    // Called by FloorplanReceiver when image processing is complete
    public void OnFloorplanProcessed(bool success)
    {
        string status = success ? "success" : "error";
        SendMessageToReact("onFloorplanLoaded", status);
        
        if (success)
        {
            Debug.Log("Floorplan successfully applied to material");
        }
        else
        {
            Debug.LogError("Failed to process floorplan");
        }
    }
}
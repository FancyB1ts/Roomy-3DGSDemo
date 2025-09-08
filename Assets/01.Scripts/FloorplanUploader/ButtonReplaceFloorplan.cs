using UnityEngine;

public class ButtonReplaceFloorplan : MonoBehaviour
{
    [Header("Bridge Reference")]
    public UnityWebGLBridge webGLBridge;
    
    void Start()
    {
        // Validate reference
        if (webGLBridge == null)
        {
            Debug.LogError("ButtonReplaceFloorplan: webGLBridge not assigned!");
        }
    }
    
    // Public method to be called by UI Button's OnClick event
    public void OnReplaceFloorplanClicked()
    {
        Debug.Log("ButtonReplaceFloorplan: Replace floorplan button clicked");
        
        if (webGLBridge != null)
        {
            webGLBridge.ShowFloorplanUploader();
        }
        else
        {
            Debug.LogError("ButtonReplaceFloorplan: Cannot show uploader - webGLBridge is null!");
        }
    }
}
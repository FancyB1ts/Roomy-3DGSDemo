using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class EmailLinkHandler : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text tmp;
    private Camera uiCam; // for non-overlay canvases
    [SerializeField] private bool logMissesInEditor = false;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();

        // Determine which camera to use for hit-testing
        var canvas = tmp.canvas;
        uiCam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        // Ensure clicks are received on this text
        tmp.raycastTarget = true;
    }

    /// <summary>
    /// Handles clicks on TMP <link> regions that are already present in the text.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (tmp == null) return;

        tmp.ForceMeshUpdate();

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmp, eventData.position, eventData.pressEventCamera != null ? eventData.pressEventCamera : uiCam);
#if UNITY_EDITOR
        if (logMissesInEditor && linkIndex == -1)
            Debug.Log("EmailLinkHandler: click did not hit a TMP <link> region.");
#endif
        if (linkIndex == -1) return;

        var linkInfo = tmp.textInfo.linkInfo[linkIndex];
        string linkId = linkInfo.GetLinkID(); // e.g., "mailto:feedback@roomy-app.co" or any other URL
        if (!string.IsNullOrEmpty(linkId))
        {
            Debug.Log($"EmailLinkHandler: opening link {linkId}");
            Application.OpenURL(linkId);
        }
    }
}
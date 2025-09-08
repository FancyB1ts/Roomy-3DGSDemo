using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ScrollImageUV : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public string uvOffsetProperty = "_UVOffset";

    private Material runtimeMat;
    private Vector2 totalOffset = Vector2.zero;
    public float offsetMultiplier = 1f;
    public float cameraOffsetMultiplier = 1f;
    private Vector2 lastMousePos;
    private bool dragging = false;
    public CameraMovement camController;
    public float camMouseDelta;
    Vector2 startMousePos;
    void Awake()
    {
        Image img = GetComponent<Image>();
        // Clone the material to avoid modifying the shared one
        runtimeMat = Instantiate(img.material);
        img.material = runtimeMat;
        if(!camController)
        {
            camController= FindAnyObjectByType<CameraMovement>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            dragging = true;
            lastMousePos = eventData.position;
            startMousePos = eventData.position;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        Vector2 currentMousePos = eventData.position;
        Vector2 delta = currentMousePos - lastMousePos;

        // Normalize to screen width to make it resolution independent
        float uvDeltaX = delta.x / Screen.width;

        // Update shader UV offset
        totalOffset.x += uvDeltaX * offsetMultiplier;
        runtimeMat.SetVector(uvOffsetProperty, new Vector4(totalOffset.x, 0, 0, 0));

        // Update camera rotation
        if (camController)
        {
            float signedDelta = delta.x;
            camMouseDelta = signedDelta * cameraOffsetMultiplier;
            camController.updateVerticalRotation(camMouseDelta);
        }

        lastMousePos = currentMousePos;
    }
}

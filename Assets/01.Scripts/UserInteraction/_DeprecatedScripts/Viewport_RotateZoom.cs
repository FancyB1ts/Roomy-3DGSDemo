using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ViewportRotateZoom : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 0.2f;
    public float minY = -180f;
    public float maxY = 180f;
    public float minX = -10f;
    public float maxX = 10f;

    [Header("Zoom Settings (Percent-based)")]
    public Camera orthoCamera; // Assign your isometric RenderTexture camera here
    public float zoomSpeed = 0.5f;
    public float minZoomPercent = 100f;
    public float maxZoomPercent = 200f;

    [Header("Container Control")]
    [Tooltip("Assign the parent UI GameObject (e.g. your panel or screen under the Canvas) that toggles this component on/off.")]
    public GameObject container; // Must be set in the Inspector: drag in the UI container that enables/disables this view

    private float baseOrthoSize;
    private float currentZoomPercent = 100f;

    private Vector2 lastPointerPos;
    private bool isDragging;

    void Start()
    {
        if (orthoCamera != null)
            baseOrthoSize = orthoCamera.orthographicSize;
    }

    void Update()
    {
        // If a container is assigned and inactive, skip all input
        if (container != null && !container.activeInHierarchy)
            return;

#if UNITY_EDITOR
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastPointerPos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        if (isDragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastPointerPos;
            ApplyRotation(delta);
            lastPointerPos = Input.mousePosition;
        }
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Zoom(-scroll * 100f);
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
                ApplyRotation(touch.deltaPosition);
        }
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
            float currDist = (t0.position - t1.position).magnitude;
            Zoom(currDist - prevDist);
        }
    }

    private void ApplyRotation(Vector2 delta)
    {
        Vector3 euler = transform.rotation.eulerAngles;
        float newY = NormalizeAngle(euler.y - delta.x * rotationSpeed);
        float newX = NormalizeAngle(euler.x + delta.y * rotationSpeed);
        newY = Mathf.Clamp(newY, minY, maxY);
        newX = Mathf.Clamp(newX, minX, maxX);
        transform.rotation = Quaternion.Euler(newX, newY, 0f);
    }

    private void Zoom(float delta)
    {
        currentZoomPercent = Mathf.Clamp(currentZoomPercent + delta * zoomSpeed, minZoomPercent, maxZoomPercent);
        if (orthoCamera != null)
            orthoCamera.orthographicSize = baseOrthoSize * (currentZoomPercent / 100f);
    }

    private float NormalizeAngle(float angle)
    {
        return (angle > 180f) ? angle - 360f : angle;
    }
}

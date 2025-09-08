using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles input detection for camera controls only.
/// Detects mouse panning and wheel zoom when not over UI or furniture.
/// </summary>
public class CameraInputManager : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private LayerMask furnitureLayerMask;
    [SerializeField] private MousePlacementManager mousePlacementManager;

    [Header("Camera Zoom Settings")]
    [SerializeField] private float zoomSensitivity = 0.5f;
    [SerializeField] private float maxZoomPerFrame = 2f;

    // Touch input variables
    private bool isTouchPanning = false;
    private Vector2 lastTouchPosition;
    private bool hadTwoTouchesLastFrame = false;

    [Header("Touch Gestures")]
    [SerializeField] private bool touchZoomFollowsFingers = true; // spread = zoom in, pinch = zoom out
    [SerializeField] private bool touchRotationFollowsFingers = true; // twist direction follows finger rotation
    [SerializeField] private bool invertPanDuringPinch = true; // pan opposite to finger movement so content stays under fingers
    [SerializeField] private float pinchPanDampen = 1.0f; // scale for compensated pan during pinch
    [SerializeField] private float pinchMidSpikeSqrPx = 9f; // clamp large first-frame midpoint spikes (~3px^2)
    private bool isPinching = false;
    private Vector2 pinchPrevMid;
    private Vector2 pinchPrevVec; // previous vector between two touches
    private float pinchPrevDistance = 0f;
    private const float pinchDeadzonePx = 0.75f; // small noise filter
    private int activeFingerId = -1; // track the current 1-finger pan finger safely

    // Camera Events
    public event Action OnCameraPanStarted, OnCameraPanFinished;
    public event Action<Vector2> OnCameraPan;
    public event Action<float> OnCameraZoom;
    public event Action<float> OnCameraRotate; // degrees per frame (two-finger twist)

    private bool isPanning = false;
    private Vector3 lastPanPosition;

    // ---- Furniture ownership gates (from MousePlacementManager) ----
    private bool WheelLockedToFurniture()
        => mousePlacementManager != null && mousePlacementManager.IsWheelLockedToFurniture;
    private bool FurnitureHasFocus()
        => mousePlacementManager != null && mousePlacementManager.HasFocusedFurniture;

    // ---- Camera wheel ownership (mirrors furniture lock) ----
    [Header("Wheel Ownership (Camera)")]
    [SerializeField] private float cameraWheelLockMs = 400f; // how long camera keeps wheel ownership after zooming
    private double cameraWheelLockExpiryMs = 0;
    public bool IsWheelLockedToCamera => NowMs() <= cameraWheelLockExpiryMs;

    // local time helper (milliseconds)
    private static double NowMs() => Time.realtimeSinceStartupAsDouble * 1000.0;

     void Start()
    {
        // Disable Unity Input System's automatic touch processing
        #if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
        #endif
    }

    private void Update()
    {
        HandleMouseInput();
        HandleScrollInput();
        HandleTouchInput();
    }

    private void HandleMouseInput()
    {
        // Start panning on left mouse button down
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsInteractingWithUI())
            {
                // Wait one frame to see if furniture system will handle the input
                StartCoroutine(CheckForCameraPan());
            }
        }

        // Handle ongoing panning
        if (isPanning && Input.GetMouseButton(0))
        {
            Vector3 currentPos = Input.mousePosition;
            Vector2 panDelta = currentPos - lastPanPosition;
            OnCameraPan?.Invoke(panDelta);
            lastPanPosition = currentPos;
        }

        // Stop panning on mouse button up
        if (Input.GetMouseButtonUp(0))
        {
            if (isPanning)
            {
                isPanning = false;
                OnCameraPanFinished?.Invoke();
            }
        }
    }

    private void HandleScrollInput()
    {
        float normalizedScroll = GetNormalizedCameraScroll();
        if (Mathf.Abs(normalizedScroll) <= 0.01f) return;

        // Never zoom camera while interacting with UI
        if (IsInteractingWithUI()) return;

        // Respect furniture ownership unless camera already owns the wheel (keep refreshing during burst)
        if (!IsWheelLockedToCamera && (WheelLockedToFurniture() || FurnitureHasFocus())) return;

        // Also ignore legacy blocks while camera owns the wheel
        if (!IsWheelLockedToCamera && (IsOverExistingFurniture() || IsFurnitureSystemActive())) return;

        // Otherwise, apply camera zoom
        OnCameraZoom?.Invoke(normalizedScroll);
        // Acquire/refresh camera wheel ownership
        cameraWheelLockExpiryMs = NowMs() + cameraWheelLockMs;
    }

    private float GetNormalizedCameraScroll()
    {
        float rawScroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(rawScroll) < 0.01f) return 0f;
        
        return Mathf.Clamp(rawScroll * zoomSensitivity, -maxZoomPerFrame, maxZoomPerFrame);
    }

    private IEnumerator CheckForCameraPan()
    {
        // Wait two frames to give furniture system first right of refusal
        yield return null;
        yield return null;

        bool furnitureActive = IsFurnitureInteractionActive();
        bool overExistingFurniture = IsOverExistingFurniture();
        bool hasFocus = FurnitureHasFocus();

        // Don't start camera pan if over furniture, furniture system is active, or furniture has focus
        if (!furnitureActive && !overExistingFurniture && !hasFocus)
        {
            isPanning = true;
            lastPanPosition = Input.mousePosition;
            OnCameraPanStarted?.Invoke();
        }
    }

    /// <summary>
    /// Checks if mouse is over existing furniture objects
    /// </summary>
    private bool IsOverExistingFurniture()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = sceneCamera.nearClipPlane;
        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        
        // Raycast specifically for furniture objects
        if (Physics.Raycast(ray, out RaycastHit hit, 100, furnitureLayerMask))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if furniture placement system is currently active
    /// </summary>
    private bool IsFurnitureInteractionActive()
    {
        return mousePlacementManager != null && 
               (mousePlacementManager.state == MousePlacementManager.PlacementState.Creating ||
                mousePlacementManager.state == MousePlacementManager.PlacementState.Moving);
    }

    /// <summary>
    /// Checks if mouse is over UI elements
    /// </summary>
    private bool IsInteractingWithUI()
    {
        return EventSystem.current && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Gets the UI element under the mouse pointer (for debugging)
    /// </summary>
    public static GameObject GetUIElementUnderPointer()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        if (raycastResults.Count > 0)
        {
            return raycastResults[0].gameObject; // Top-most UI element
        }

        return null;
    }

    /// <summary>
    /// Checks if any furniture interaction system is currently active
    /// </summary>
    private bool IsFurnitureSystemActive()
    {
        // Check if MousePlacementManager is in an active state
        if (mousePlacementManager != null)
        {
            return mousePlacementManager.state != MousePlacementManager.PlacementState.Idle &&
                mousePlacementManager.state != MousePlacementManager.PlacementState.Placed;
        }
        
        return false;
    }

private void HandleTouchInput()
{
    bool twoTouchesThisFrame = Input.touchCount >= 2;

    if (twoTouchesThisFrame)
    {
        // Cancel any ongoing single-finger pan immediately when second finger appears
        if (isTouchPanning)
        {
            isTouchPanning = false;
        }

        HandlePinch();
    }
    else
    {
        // If we drop below two fingers, end pinch cleanly
        if (isPinching)
        {
            isPinching = false;
            pinchPrevDistance = 0f;
            pinchPrevVec = Vector2.zero;
        }

        // If pinch just ended and one finger remains, immediately hand off to 1-finger pan to avoid a snap
        if (!twoTouchesThisFrame && isTouchPanning == false && Input.touchCount == 1)
        {
            // Only start if not over UI/furniture
            if (!IsInteractingWithUI() && !IsOverExistingFurniture() && !IsFurnitureSystemActive() && !WheelLockedToFurniture() && !FurnitureHasFocus())
            {
                if (TouchSafe.TryGetTouch(0, out var t))
                {
                    isTouchPanning = true;
                    activeFingerId = t.fingerId;
                    lastTouchPosition = t.position;
                    OnCameraPanStarted?.Invoke();
                }
            }
        }

        // Single-finger paths
        if (Input.touchCount == 1)
        {
            HandleSingleTouch();
        }
        else if (Input.touchCount == 0 && isTouchPanning)
        {
            // Stop touch panning when no touches
            isTouchPanning = false;
            activeFingerId = -1;
            OnCameraPanFinished?.Invoke();
        }
    }

    hadTwoTouchesLastFrame = twoTouchesThisFrame;
}

private void HandleSingleTouch()
{
    if (Input.touchCount == 0) return;
    if (Input.touchCount > 1) return;

    // Resolve the active finger if we have one; otherwise read the first touch safely
    Touch touch;
    if (activeFingerId >= 0)
    {
        if (!TouchSafe.TryFindByFingerId(activeFingerId, out touch))
        {
            // the tracked finger ended; stop panning if it was active
            if (isTouchPanning)
            {
                isTouchPanning = false;
                activeFingerId = -1;
                OnCameraPanFinished?.Invoke();
            }
            return;
        }
    }
    else
    {
        if (!TouchSafe.TryGetTouch(0, out touch)) return;
    }

    // Touch began = mouse down
    if (touch.phase == TouchPhase.Began && !IsInteractingWithUI())
    {
        activeFingerId = touch.fingerId;
        StartCoroutine(CheckForCameraTouchPan());
    }

    // Touch moved = mouse drag while panning
    if (touch.phase == TouchPhase.Moved && isTouchPanning)
    {
        Vector2 touchDelta = touch.position - lastTouchPosition;
        Vector2 adjustedDelta = touchDelta * 0.15f;
        OnCameraPan?.Invoke(adjustedDelta);
        lastTouchPosition = touch.position;
    }

    // Touch ended/canceled = mouse up
    if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
    {
        if (isTouchPanning)
        {
            isTouchPanning = false;
            OnCameraPanFinished?.Invoke();
        }
        activeFingerId = -1;
    }
}

private void HandlePinch()
{
    // Read first two touches safely
    if (!TouchSafe.TryGetTwoTouches(out var touch1, out var touch2))
    {
        // end pinch gracefully if one finger dropped between frames
        isPinching = false;
        pinchPrevDistance = 0f;
        pinchPrevVec = Vector2.zero;
        return;
    }

    // If interacting with UI, do nothing
    if (IsInteractingWithUI()) return;

    // Respect furniture guards (same as old implementation)
    if (IsOverExistingFurniture() || IsFurnitureSystemActive() || WheelLockedToFurniture() || FurnitureHasFocus())
    {
        // Reset pinch baseline so we don't accumulate a big delta when guards clear
        isPinching = false;
        pinchPrevDistance = 0f;
        return;
    }

    // Current midpoint & distance
    Vector2 mid = (touch1.position + touch2.position) * 0.5f;
    float currDistance = Vector2.Distance(touch1.position, touch2.position);

    // Initialize pinch on first valid frame
    if (!isPinching || pinchPrevDistance <= 0f || touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
    {
        isPinching = true;
        pinchPrevMid = mid;
        pinchPrevDistance = currDistance;
        pinchPrevVec = touch2.position - touch1.position;
        return; // wait next frame for a stable delta
    }

    // Compute deltas
    float deltaDistance = currDistance - pinchPrevDistance;
    Vector2 midDelta = mid - pinchPrevMid;

    // Guard against initial-frame spikes right after second finger lands
    if (midDelta.sqrMagnitude > pinchMidSpikeSqrPx)
    {
        midDelta = Vector2.zero;
    }

    // Small deadzone to avoid micro-jitter (check both zoom and pan deltas)
    Vector2 avgDeltaForDeadzone = (touch1.deltaPosition + touch2.deltaPosition) * 0.5f;
    if (Mathf.Abs(deltaDistance) < pinchDeadzonePx && avgDeltaForDeadzone.sqrMagnitude < (pinchDeadzonePx * pinchDeadzonePx))
    {
        // Update baselines but skip actions this frame
        pinchPrevMid = mid;
        pinchPrevDistance = currDistance;
        // keep pinchPrevVec as-is so rotation baseline remains stable
        return;
    }

    // Normalize zoom similar to mouse path, but using the same sensitivity (per your preference)
    float normalizedZoom = deltaDistance * 0.01f; // base scaling
    normalizedZoom = Mathf.Clamp(normalizedZoom * zoomSensitivity, -maxZoomPerFrame, maxZoomPerFrame);

    // Ensure pinch direction follows fingers (spread -> zoom in)
    if (!touchZoomFollowsFingers)
    {
        normalizedZoom = -normalizedZoom;
    }

    // Emit zoom first
    if (Mathf.Abs(normalizedZoom) > 0.001f)
    {
        OnCameraZoom?.Invoke(normalizedZoom);
    }

    // Pan using the average of both finger deltas (avoids centroid re-anchor jump)
    Vector2 avgDelta = (touch1.deltaPosition + touch2.deltaPosition) * 0.5f;
    if (avgDelta.sqrMagnitude > 0.01f)
    {
        Vector2 compensatedPan = (invertPanDuringPinch ? -avgDelta : avgDelta) * pinchPanDampen;
        OnCameraPan?.Invoke(compensatedPan);
    }

    // --- Two-finger twist rotation ---
    Vector2 currVec = touch2.position - touch1.position;
    if (pinchPrevVec.sqrMagnitude > 0.0001f && currVec.sqrMagnitude > 0.0001f)
    {
        float prevAngle = Mathf.Atan2(pinchPrevVec.y, pinchPrevVec.x) * Mathf.Rad2Deg;
        float currAngle = Mathf.Atan2(currVec.y, currVec.x) * Mathf.Rad2Deg;
        float deltaAngle = Mathf.DeltaAngle(prevAngle, currAngle);
        if (!touchRotationFollowsFingers) deltaAngle = -deltaAngle;
        if (Mathf.Abs(deltaAngle) > 0.01f && hadTwoTouchesLastFrame)
        {
            OnCameraRotate?.Invoke(deltaAngle);
        }
    }

    // Update baselines
    pinchPrevMid = mid;
    pinchPrevDistance = currDistance;
    pinchPrevVec = currVec;
}

private IEnumerator CheckForCameraTouchPan()
{
    // Wait two frames to give furniture system first right of refusal
    yield return null;
    yield return null;
    
    bool furnitureActive = IsFurnitureInteractionActive();
    bool overExistingFurniture = IsOverExistingFurniture();
    bool hasFocus = FurnitureHasFocus();
    
    // Don't start camera pan if over furniture, furniture system is active, or furniture has focus
    if (!furnitureActive && !overExistingFurniture && !hasFocus)
    {
        if (TouchSafe.TryGetTouch(0, out var t))
        {
            isTouchPanning = true;
            activeFingerId = t.fingerId;
            lastTouchPosition = t.position;
            OnCameraPanStarted?.Invoke();
        }
    }
}
    
}
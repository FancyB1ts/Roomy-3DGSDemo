using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;
using System.Linq;
using Slicer;

public class MousePlacementManager : MonoBehaviour
{
    private HashSet<GameObject> furnitureRoots = new HashSet<GameObject>();
    private Vector3 dragOffset;
    public GameObject[] prefabArray;
    public LayerMask placementLayer, furnitureLayer;
    public LayerMask collisionLayer;
    public Material defaultInvalidMaterial;
    private FurnitureUIController activeUIController;

    [Header("Rotation Settings")]
    public float rotationAmount = 10f;
    public float movementSpeed = 10f;

    [Header("Events")]
    public UnityEvent OnRotationStart;
    public UnityEvent OnRotationEnd;
    public UnityEvent OnMovementStart;
    public UnityEvent OnMovementEnd;

    private GameObject currentObject;
    private List<Renderer> currentRenderers = new();
    private Dictionary<Renderer, Material[]> originalMaterials = new();
    private Collider currentCollider;
    private bool isColliding = false;
    private bool hasRotated = false;
    private bool isMoving = false;
    private FurnitureUIController previousUIController = null;
    private float preservedHeightOffset = 0f;

    public enum PlacementState { Idle, Creating, Placed, Moving, WaitPointerUp, waitingForUI }
    public PlacementState state = PlacementState.Idle;

    private Camera mainCam;
    private bool pointerWasDownLastFrame = false;

    [Header("Tap/Drag Hysteresis")]
    [SerializeField] private float mouseSlopPx = 6f;
    [SerializeField] private float mouseVelThreshPxPerMs = 0.25f;
    [SerializeField] private float touchSlopPx = 14f;
    [SerializeField] private float touchVelThreshPxPerMs = 0.35f;

    private bool awaitingIntent = false;   // true after pointer/touch down on furniture until we decide tap vs drag
    private Vector2 downPos;
    private Vector2 prevPos;
    private float pathLen = 0f;
    private float peakVel = 0f;
    private double lastUpdateMs = 0;

    // Track camera wheel lock to detect unlock transition
    private bool cameraLockWasActive = false;

    private bool isTouchActive = false;
    private Vector2 currentTouchPosition;
    private int activeTouchId = -1;
    private bool touchStarted = false;

    [Header("Ownership Timers (routing only)")]
    [SerializeField] private float hoverGraceMs = 450f;   // Keep furniture focus briefly after ray slips off
    [SerializeField] private float wheelLockMs = 750f;    // Keep wheel ownership on furniture during a scroll burst
    [SerializeField] private CameraInputManager cameraInputManager;

    // Internal state for ownership logic
    private GameObject focusedFurniture;                  // Last furniture under cursor (root)
    private double hoverGraceExpiryMs = 0;                // Time in ms when hover focus expires
    private double wheelLockExpiryMs = 0;                 // Time in ms when wheel lock expires

    // Public read-only getters for other systems (camera, etc.)
    public bool HasFocusedFurniture => (focusedFurniture != null) || (TimeInMs() <= hoverGraceExpiryMs);
    public bool IsWheelLockedToFurniture => TimeInMs() <= wheelLockExpiryMs;
    public GameObject FocusedFurnitureRoot => focusedFurniture ? GetFurnitureRoot(focusedFurniture) : null;

    void Awake()
    {
        mainCam = Camera.main;
    }

    private static double TimeInMs() => Time.time * 1000.0;

    // Consider touch only on actual mobile, or when there is an active touch this frame (hybrid laptops stay desktop)
    private bool IsTouchDevice()
    {
        return Application.isMobilePlatform || Input.touchCount > 0;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && activeUIController != null && activeUIController.isDataUIOpen)
        {
            if (!IsPointerOverFurnitureUI())
            {
                activeUIController.DisableDataUI();
                activeUIController = null;
            }
        }

        // Close furniture UI if scrolling outside of it
        if (!Mathf.Approximately(Input.mouseScrollDelta.y, 0f) && activeUIController != null && activeUIController.isDataUIOpen)
        {
            if (!IsPointerOverFurnitureUI())
            {
                activeUIController.DisableDataUI();
                activeUIController = null;
            }
        }

        // Capture camera lock state at the start of Update
        bool cameraLockNow = cameraInputManager != null && cameraInputManager.IsWheelLockedToCamera;
        double nowMs = TimeInMs();
        float dtMs = lastUpdateMs > 0 ? (float)(nowMs - lastUpdateMs) : 16f;
        
        // Update instantaneous hover focus with grace (no dwell; just delayed release)
        UpdateHoverFocus();

        // Refresh rotation UI if camera wheel lock just ended and still hovering furniture
        if (cameraLockWasActive && !cameraLockNow)
        {
            // If we just unlocked camera wheel and are still hovering furniture, light up the rotation UI once
            if (!Input.GetMouseButton(0) && !IsPointerOverUI())
            {
                Ray rayUnlock = mainCam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(rayUnlock, out RaycastHit hitUnlock, 100f, furnitureLayer))
                {
                    currentObject = hitUnlock.collider.gameObject;
                    var newUIController = currentObject.GetComponent<FurnitureUIController>();

                    if (newUIController != uiController)
                    {
                        if (uiController != null) uiController.DisableRotationUI();
                        uiController = newUIController;
                    }

                    if (uiController != null)
                    {
                        if (!IsTouchDevice()) uiController.EnableRotationUI();
                        else uiController.DisableRotationUI();
                    }
                }
            }
        }

        switch (state)
        {
            case PlacementState.Creating:
                FollowMouseAndPlace();
                HandleCollisionCheck();
                HandleRotationWithScroll();
                if (Input.GetMouseButtonDown(0) && !isColliding && !IsPointerOverUI())
                {
                    ResetPlacement();
                }
                break;

            case PlacementState.WaitPointerUp:
                if (!Input.GetMouseButton(0))
                    state = PlacementState.Placed;
                break;

            case PlacementState.Placed:
                if (!Input.GetMouseButton(0) && !IsPointerOverUI() && (!uiController || (uiController && !uiController.isDataUIOpen)))
                {
                    Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 100f, furnitureLayer))
                    {
                        currentObject = hit.collider.gameObject;
                        var newUIController = currentObject.GetComponent<FurnitureUIController>();

                        // Only change UI if we're hovering over a different furniture piece
                        if (newUIController != uiController)
                        {
                            // Disable previous rotation UI
                            if (uiController != null)
                            {
                                uiController.DisableRotationUI();
                            }

                        // Set new UI controller and enable its rotation UI (suppressed during camera wheel lock)
                        uiController = newUIController;
                        if (uiController != null)
                        {
                            if (cameraInputManager != null && cameraInputManager.IsWheelLockedToCamera)
                            {
                                uiController.DisableRotationUI();
                            }
                            else if (!IsTouchDevice())
                            {
                                uiController.EnableRotationUI();
                            }
                            else
                            {
                                uiController.DisableRotationUI();
                            }
                        }
                        }

                        currentRenderers.Clear();
                        originalMaterials.Clear();
                        if (currentObject)
                            currentRenderers.AddRange(currentObject.GetComponentsInChildren<Renderer>().Where(x => !(x is SpriteRenderer)));

                        foreach (var renderer in currentRenderers)
                        {
                            originalMaterials[renderer] = renderer.materials;
                        }
                    }
                    // Raycast miss
                    else
                    {
                        if (uiController != null)
                        {
                            if (cameraInputManager != null && cameraInputManager.IsWheelLockedToCamera)
                            {
                                // While camera owns wheel, keep rotation UI hidden
                                uiController.DisableRotationUI();
                            }
                            else if (IsTouchDevice())
                            {
                                // On touch devices, rotation UI is never shown
                                uiController.DisableRotationUI();
                            }
                            else if (IsWheelLockedToFurniture || HasFocusedFurniture)
                            {
                                // Still in grace/lock → keep the rotation UI alive (desktop only)
                                uiController.EnableRotationUI();
                            }
                            else
                            {
                                // No grace/lock → hide it as usual
                                uiController.DisableRotationUI();
                                uiController = null;
                            }
                        }
                    }
                    HandleRotationWithScroll();
                }
                if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                {
                    Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 100f, furnitureLayer))
                    {
                        selectedObject = hit.collider.gameObject;
                        currentObject = selectedObject;
                        uiController = currentObject.GetComponent<FurnitureUIController>();
                        if (uiController) uiController.DisableRotationUI();

                        // Begin intent hysteresis
                        awaitingIntent = true;
                        downPos = Input.mousePosition;
                        prevPos = downPos;
                        pathLen = 0f;
                        peakVel = 0f;
                    }
                }
                // Hysteresis processing for mouse while button held
                if (awaitingIntent)
                {
                    if (Input.GetMouseButton(0))
                    {
                        Vector2 cur = (Vector2)Input.mousePosition;
                        Vector2 d = cur - prevPos;
                        pathLen += d.magnitude;
                        if (dtMs > 0f)
                        {
                            float vel = d.magnitude / dtMs; // px per ms
                            if (vel > peakVel) peakVel = vel;
                        }
                        prevPos = cur;

                        float slop = Application.isMobilePlatform ? touchSlopPx : mouseSlopPx;
                        float vthr = Application.isMobilePlatform ? touchVelThreshPxPerMs : mouseVelThreshPxPerMs;
                        if (pathLen > slop || peakVel > vthr)
                        {
                            // Promote to drag
                            OnLongPress(selectedObject);
                            awaitingIntent = false;
                        }
                    }
                    else if (Input.GetMouseButtonUp(0))
                    {
                        // Treat as tap if still inside hysteresis
                        if (!IsPointerOverUI() && selectedObject != null)
                        {
                            uiController = selectedObject.GetComponent<FurnitureUIController>();
                            if (uiController != null)
                            {
                                Vector3 screenPosition = Input.mousePosition;
                                uiController.EnableDataUI(screenPosition);
                                activeUIController = uiController;
                            }
                        }
                        awaitingIntent = false;
                    }
                }
                break;

            case PlacementState.Moving:
                FollowMouseAndPlace();
                HandleCollisionCheck();


                if (Input.GetMouseButtonUp(0) && !isColliding && !IsPointerOverUI())
                {
                    ResetPlacement();
                    OnMovementEnd?.Invoke();
                    isMoving = false;
                    if (uiController != null)
                    {
                        uiController.DisableMovementUI();
                        uiController.DisableRotationUI();
                        activeUIController = null;
                        uiController = null;
                    }
                    currentRenderers.Clear();
                    originalMaterials.Clear();
                    if (currentObject == null)
                    {
                        currentObject = selectedObject;
                    }
                    if (currentObject)
                        currentRenderers.AddRange(currentObject.GetComponentsInChildren<Renderer>().Where(x => !(x is SpriteRenderer)));
                    else
                    {
                        Debug.LogWarning("Current object is null, cannot add renderers.");
                    }
                    foreach (var renderer in currentRenderers)
                    {
                        originalMaterials[renderer] = renderer.materials;
                    }

                }
                break;
            case PlacementState.waitingForUI:
                if (!Input.GetMouseButton(0) || uiController == null || !uiController.isDataUIOpen)
                    state = PlacementState.Idle;

                break;

        }
            HandleTouchInput();
            
            lastUpdateMs = nowMs;
            cameraLockWasActive = cameraLockNow;
            pointerWasDownLastFrame = Input.GetMouseButton(0);
    }
    public FurnitureUIController uiController;
    public GameObject selectedObject;

    private void OnLongPress(GameObject selectedGO)
    {
        var selected = selectedGO;
        currentObject = selected;
        currentCollider = currentObject.GetComponentInChildren<Collider>();
        state = PlacementState.Moving;
        focusedFurniture = GetFurnitureRoot(selectedGO);
        hoverGraceExpiryMs = TimeInMs() + hoverGraceMs;

        // Calculate initial drag offset
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementLayer))
        {
            GameObject furnitureRoot = GetFurnitureRoot(currentObject);
            dragOffset = furnitureRoot.transform.position - hit.point;

            // Preserve current height offset for dimension-scaled furniture
            var dimensionController = furnitureRoot.GetComponent<FurnitureDimensionController>();
            preservedHeightOffset = dimensionController != null ? dimensionController.GetHeightOffset() : 0f;
        }

        uiController = selectedGO.GetComponent<FurnitureUIController>();
        if (uiController != null)
        {
            uiController.EnableMovementUI();
            activeUIController = uiController;
        }

        OnMovementStart?.Invoke();
        isMoving = true;
    }

    public void CreateObject(int id)
    {
        if (state != PlacementState.Idle && state != PlacementState.Placed) return;
        if (uiController != null && uiController.isDataUIOpen) return;
        if (id < 0 || id >= prefabArray.Length) return;

        currentObject = Instantiate(prefabArray[id]);
        furnitureRoots.Add(currentObject);
        currentRenderers.Clear();
        originalMaterials.Clear();
        currentRenderers.AddRange(currentObject.GetComponentsInChildren<Renderer>());

        foreach (var renderer in currentRenderers)
        {
            originalMaterials[renderer] = renderer.materials;
        }

        currentCollider = currentObject.GetComponentInChildren<Collider>();
        isColliding = false;
        state = PlacementState.Creating;
    }

    void FollowMouseAndPlace()
    {
        if (currentObject == null) return;
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementLayer))
        {
            GameObject positionTarget = GetFurnitureRoot(currentObject);

            if (state == PlacementState.Moving)
            {
                // During movement, maintain the drag offset
                Vector3 targetPosition = hit.point + dragOffset;
                positionTarget.transform.position = new Vector3(targetPosition.x, preservedHeightOffset, targetPosition.z);
            }
            else
            {
                // During creation, position directly at cursor
                positionTarget.transform.position = new Vector3(hit.point.x, 0f, hit.point.z);
            }
        }
    }
    public Collider[] hits;
    public float colliderScale = 0.85f;
    void HandleCollisionCheck()
    {
        if (currentCollider == null) return;

        BoxCollider box = currentCollider as BoxCollider;
        if (box == null) return;

        // Convert local center to world position
        Vector3 worldCenter = box.transform.TransformPoint(box.center);

        // Scale size by lossyScale and colliderScale
        Vector3 scaledSize = Vector3.Scale(box.size, box.transform.lossyScale* colliderScale);
        Vector3 halfExtents = scaledSize * 0.5f;

        // Perform the overlap box check
        hits = Physics.OverlapBox(
            worldCenter,
            halfExtents,
            box.transform.rotation,
            collisionLayer
        );

        // Check for actual collisions
        isColliding = false;
        foreach (var hit in hits)
        {
            if (hit.transform != currentObject.transform && hit.transform != currentCollider.transform)
            {
                isColliding = true;
                break;
            }
        }

        foreach (var renderer in currentRenderers)
        {
            if (originalMaterials.TryGetValue(renderer, out Material[] original))
            {
                renderer.materials = isColliding
                    ? CreateInvalidMaterialArray(renderer.materials.Length)
                    : original;
            }
            else
            {
                Debug.LogWarning($"No original materials found for renderer: {renderer.name}");

            }
        }
    }
    private void OnDrawGizmos()
    {
        BoxCollider box = currentCollider as BoxCollider;
        if (box != null)
        {
            Vector3 worldCenter = box.transform.TransformPoint(box.center);
            Vector3 worldHalfExtents = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);

            Gizmos.color = Color.green;
            Matrix4x4 matrix = Matrix4x4.TRS(worldCenter, box.transform.rotation, Vector3.one);
            Gizmos.matrix = matrix;
            Gizmos.DrawWireCube(Vector3.zero, worldHalfExtents * 2);
        }
    }
    Material[] CreateInvalidMaterialArray(int length)
    {
        Material[] invalids = new Material[length];
        for (int i = 0; i < length; i++)
        {
            invalids[i] = defaultInvalidMaterial;
        }
        return invalids;
    }

    void HandleRotationWithScroll()
    {
        // Respect camera wheel ownership: if camera is currently locked to wheel, do not rotate furniture
        if (cameraInputManager != null && cameraInputManager.IsWheelLockedToCamera)
            return;
        if (currentObject == null)
        {
            Debug.Log("ERROR");
            return;
        }
        
        // Use limited scroll input
        int scrollDirection = ScrollInputLimiter.GetScrollDirection();

        // If no scroll this frame, consider ending rotation if lock expired
        if (scrollDirection == 0)
        {
            if (hasRotated && TimeInMs() > wheelLockExpiryMs)
            {
                OnRotationEnd?.Invoke();
                hasRotated = false;
            }
            return;
        }

        // Decide if furniture should receive the wheel right now
        bool furnitureOwnsWheel = IsWheelLockedToFurniture || HasFocusedFurniture;
        if (!furnitureOwnsWheel)
        {
            // Do not rotate here; allow camera system to consume the wheel
            return;
        }

        // From here on, the wheel goes to furniture rotation
        if (!hasRotated)
        {
            OnRotationStart?.Invoke();
            hasRotated = true;
        }

        // Choose the rotation target: focused furniture root if available, else current object root
        GameObject rotationTarget = FocusedFurnitureRoot;
        if (rotationTarget == null)
        {
            rotationTarget = GetFurnitureRoot(currentObject);
        }
        
        // Rotate the furniture root
        float newYRotation = rotationTarget.transform.eulerAngles.y + (scrollDirection * 11.25f);
        newYRotation = SnapToAngleIncrement(newYRotation, 11.25f);
        rotationTarget.transform.rotation = Quaternion.Euler(0f, newYRotation, 0f);

        // Refresh ownership timers on every furniture scroll tick
        double now = TimeInMs();
        wheelLockExpiryMs = now + wheelLockMs;   // continuous scrolling keeps lock alive
        hoverGraceExpiryMs = now + hoverGraceMs; // keep focus even if collider slips briefly
    }

    private void UpdateHoverFocus()
    {
        if (mainCam == null) return;

        // Suppress hover reacquisition and rotation UI entirely while camera owns the wheel
        if (cameraInputManager != null && cameraInputManager.IsWheelLockedToCamera)
        {
            if (TimeInMs() > hoverGraceExpiryMs)
            {
                focusedFurniture = null;
            }
            if (uiController != null)
            {
                uiController.DisableRotationUI();
            }
            return;
        }

        // On mobile: if there is no active touch, don't acquire/refresh hover.
        // Let existing hover grace expire naturally so UI can hide.
        if (Application.isMobilePlatform && Input.touchCount == 0)
        {
            if (TimeInMs() > hoverGraceExpiryMs)
            {
                focusedFurniture = null;
            }
            return;
        }

        // Choose pointer position: touch if present, else mouse (desktop), using index-safe touch access
        Vector3 screenPos;
        if (Input.touchCount > 0 && TouchSafe.TryGetTouch(0, out var hoverTouch))
            screenPos = hoverTouch.position;
        else
            screenPos = Input.mousePosition;

        // Raycast against furniture layer to detect immediate hover
        Ray ray = mainCam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, furnitureLayer))
        {
            var root = GetFurnitureRoot(hit.collider.gameObject);
            if (root != null)
            {
                focusedFurniture = root;
                hoverGraceExpiryMs = TimeInMs() + hoverGraceMs; // instant acquire, delayed release
            }
        }
        else
        {
            // Do not clear focusedFurniture immediately; allow it to persist until grace expires
            if (TimeInMs() > hoverGraceExpiryMs)
            {
                focusedFurniture = null;
            }
        }
    }

    void ResetPlacement()
    {
        currentObject = null;
        currentRenderers.Clear();
        originalMaterials.Clear();
        currentCollider = null;
        state = PlacementState.WaitPointerUp;
    }

    bool IsPointerOverUI()
    {
        return EventSystem.current && EventSystem.current.IsPointerOverGameObject();
    }

    private bool IsPointerOverFurnitureUI()
    {
        if (EventSystem.current == null) return false;
        
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        // Check if any of the UI elements hit belong to the active furniture UI
        foreach (RaycastResult result in raycastResults)
        {
            if (activeUIController != null && activeUIController.DataUI != null)
            {
                // Check if the clicked UI element is a child of the DataUI
                if (result.gameObject.transform.IsChildOf(activeUIController.DataUI.transform) || 
                    result.gameObject == activeUIController.DataUI)
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    public void OnFurnitureDeleted(GameObject deletedObject)
    {
        furnitureRoots.Remove(deletedObject);
        if (currentObject == deletedObject)
        {
            currentObject = null;
            currentRenderers.Clear();
            originalMaterials.Clear();
            currentCollider = null;
        }
        
        if (selectedObject == deletedObject)
        {
            selectedObject = null;
        }
        
        if (uiController != null && uiController.gameObject == deletedObject)
        {
            uiController = null;
        }
        
        if (activeUIController != null && activeUIController.gameObject == deletedObject)
        {
            activeUIController = null;
        }
        
        if (state == PlacementState.Moving || state == PlacementState.waitingForUI)
        {
            state = PlacementState.Idle;
        }
    }

    /// <summary>
    /// Gets the root furniture object from any child that was hit by raycast
    /// Uses caching for optimal performance
    /// </summary>
    private GameObject GetFurnitureRoot(GameObject hitObject)
    {
        // First: Fast naming convention check
        if (hitObject.name.StartsWith("PF_F_"))
        {
            return hitObject;
        }
        
        // Second: Check if hit object is in our furniture cache
        if (furnitureRoots.Contains(hitObject))
        {
            return hitObject;
        }
        
        // Third: Search up the hierarchy for naming convention or cached furniture root
        Transform current = hitObject.transform;
        while (current != null)
        {
            // Check naming convention first (faster)
            if (current.name.StartsWith("PF_F_"))
            {
                return current.gameObject;
            }
            
            // Check cache as backup
            if (furnitureRoots.Contains(current.gameObject))
            {
                return current.gameObject;
            }
            
            current = current.parent;
        }
        
        // Fallback: return original object if no furniture root found
        return hitObject;
    }

    /// <summary>
    /// Snaps an angle to the nearest increment (e.g., 11.25 degrees)
    /// </summary>
    private float SnapToAngleIncrement(float angle, float increment)
    {
        // Normalize angle to 0-360 range
        angle = angle % 360f;
        if (angle < 0) angle += 360f;

        // Snap to nearest increment
        float snappedAngle = Mathf.Round(angle / increment) * increment;

        // Normalize result to 0-360 range
        return snappedAngle % 360f;
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            HandleSingleTouchFurniture();
        }
        else if (Input.touchCount == 0 && isTouchActive)
        {
            HandleTouchEnd();
        }
    }

    private void HandleSingleTouchFurniture()
    {
        Touch touch;
        // Prefer the actively tracked finger, if any
        if (activeTouchId >= 0)
        {
            if (!TouchSafe.TryFindByFingerId(activeTouchId, out touch))
            {
                // Tracked finger ended between frames; end gracefully
                HandleTouchEnd();
                return;
            }
        }
        else
        {
            if (!TouchSafe.TryGetTouch(0, out touch)) return;
        }

        currentTouchPosition = touch.position;

        switch (touch.phase)
        {
            case TouchPhase.Began:
                HandleTouchBegan(touch);
                break;
            case TouchPhase.Moved:
                if (isTouchActive) HandleTouchMoved();
                break;
            case TouchPhase.Stationary:
                // no-op; wait for intent or end
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                HandleTouchEnd();
                break;
        }
    }

    private void HandleTouchBegan(Touch touch)
    {
        if (!IsPointerOverUI())
        {
            Ray ray = mainCam.ScreenPointToRay(touch.position);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, furnitureLayer))
            {
                isTouchActive = true;
                activeTouchId = touch.fingerId;
                selectedObject = hit.collider.gameObject;
                currentObject = selectedObject;
                uiController = currentObject.GetComponent<FurnitureUIController>();
                if (uiController) uiController.DisableRotationUI();

                // Begin intent hysteresis for touch
                awaitingIntent = true;
                downPos = touch.position;
                prevPos = downPos;
                pathLen = 0f;
                peakVel = 0f;
            }
        }
    }
    private void HandleTouchMoved()
    {
        if (!isTouchActive || !awaitingIntent) return;
        Vector2 cur = currentTouchPosition;
        Vector2 d = cur - prevPos;
        pathLen += d.magnitude;
        float vel = (float)(d.magnitude / Mathf.Max(1e-3f, (float)(Time.deltaTime * 1000.0f))); // px per ms
        if (vel > peakVel) peakVel = vel;
        prevPos = cur;

        float slop = touchSlopPx;
        float vthr = touchVelThreshPxPerMs;
        if (pathLen > slop || peakVel > vthr)
        {
            OnLongPressTouch(selectedObject);
            awaitingIntent = false;
        }
    }

    private void HandleTouchEnd()
    {
        if (!isTouchActive) return;

        if (awaitingIntent && selectedObject != null)
        {
            // Treat as tap
            uiController = selectedObject.GetComponent<FurnitureUIController>();
            if (uiController != null)
            {
                Vector3 screenPosition = currentTouchPosition;
                uiController.EnableDataUI(screenPosition);
                activeUIController = uiController;
            }
        }

        isTouchActive = false;
        activeTouchId = -1;
        awaitingIntent = false;
    }

    private void OnLongPressTouch(GameObject selectedGO)
    {
        var selected = selectedGO;
        currentObject = selected;
        currentCollider = currentObject.GetComponentInChildren<Collider>();
        state = PlacementState.Moving;
        focusedFurniture = GetFurnitureRoot(selectedGO);
        hoverGraceExpiryMs = TimeInMs() + hoverGraceMs;

        uiController = selectedGO.GetComponent<FurnitureUIController>();
        if (uiController != null)
        {
            uiController.EnableMovementUI();
            activeUIController = uiController;
        }

        OnMovementStart?.Invoke();
        isMoving = true;
    }

}
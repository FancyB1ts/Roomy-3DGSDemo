using DG.Tweening;
using Sirenix.OdinInspector;
using Slicer;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(SlicerController))]
public class FurnitureData : MonoBehaviour
{
    public GameObject rotationUI;
    public GameObject movementUI;
    public GameObject furnitureUI;
    public float rotationSpeed = 12.5f;
    public bool enableInteraction = true;
    public Renderer rendererer;
    public TMPro.TMP_InputField inputFieldX, inputFieldY, inputFieldZ;
    public SlicerController slicerController;
    public ItemData itemData; // Assign per instance!

    private FurnitureDataInputHandler inputHandler;

    // --- grid/placement managers ---
    private PlacementGridData objectPlacementData;
    private GridManager gridManager;

    // Move state
    private bool isMouseDown = false;
    private float clickDuration = 0f;
    private bool moving = false;
    private Vector3Int originalGridPos;
    private Quaternion originalRotation;
    private bool originalPositionStored = false;

    [HorizontalGroup("ObjectDimensions")]
    public int objectDimensionsX;
    [HorizontalGroup("ObjectDimensions")]
    public int objectDimensionsY;
    [HorizontalGroup("ObjectDimensions")]
    public int objectDimensionsZ;

    // Touch input variables
    private bool isTouchInteracting = false;
    private int touchId = -1;
    private float touchStartTime = 0f;

    private Collider col;

    private void Start()
    {
        var placementManager = FindAnyObjectByType<PlacementManager>();
        if (placementManager != null)
        {
            objectPlacementData = placementManager.gridData.ObjectPlacementData;
            gridManager = placementManager.gridManager;
            if (itemData == null)
                itemData = placementManager.itemData;
        }
        else
        {
            Debug.LogError("FurnitureData: No PlacementManager found in scene! Can't move object correctly.");
        }

        if (!slicerController)
            slicerController = GetComponent<SlicerController>();

        col = GetComponent<Collider>();
        CheckSize();

        if (inputFieldX)
        {
            inputFieldX.text = ((int)objectDimensionsX).ToString();
            inputFieldX.onValueChanged.AddListener(value => CalculateScale());
        }
        if (inputFieldY)
        {
            inputFieldY.text = ((int)objectDimensionsY).ToString();
            inputFieldY.onValueChanged.AddListener(value => CalculateScale());
        }
        if (inputFieldZ)
        {
            inputFieldZ.text = ((int)objectDimensionsZ).ToString();
            inputFieldZ.onValueChanged.AddListener(value => CalculateScale());
        }

        if (!rendererer)
            rendererer = GetComponentInChildren<Renderer>();
        if (rendererer)
        {
            inputHandler = rendererer.GetComponent<FurnitureDataInputHandler>();
            if (!inputHandler)
            {
                inputHandler = rendererer.gameObject.AddComponent<FurnitureDataInputHandler>();
                inputHandler.setup(this);
            }
        }

        // Register with grid at start!
        Vector3Int atGrid = gridManager.GetCellPosition(transform.position, PlacementType.FreePlacedObject);
        int yRot = Mathf.RoundToInt(transform.eulerAngles.y);
        objectPlacementData.AddCellObject(gameObject.GetInstanceID(), itemData.ID, atGrid, itemData.size, yRot, yRot);

        // Tag your furniture for overlap check
        gameObject.tag = "Furniture";
    }

    [Button]
    public void CheckSize()
    {
        if (!rendererer)
            rendererer = GetComponentInChildren<Renderer>();
        if (rendererer)
        {
            objectDimensionsX = Mathf.RoundToInt(rendererer.bounds.size.x * 100f);
            objectDimensionsY = Mathf.RoundToInt(rendererer.bounds.size.y * 100f);
            objectDimensionsZ = Mathf.RoundToInt(rendererer.bounds.size.z * 100f);
            if (inputFieldX)
                inputFieldX.text = objectDimensionsX.ToString();
            if (inputFieldY)
                inputFieldY.text = objectDimensionsY.ToString();
            if (inputFieldZ)
                inputFieldZ.text = objectDimensionsZ.ToString();
        }
    }

    public void CalculateScale()
    {
        int x = -1, y = -1, z = -1;
        int.TryParse(inputFieldX.text, out x);
        int.TryParse(inputFieldY.text, out y);
        int.TryParse(inputFieldZ.text, out z);

        Vector3 newSize = new Vector3(x, y, z);
        const float EPS = 1e-5f;
        Vector3 result = new Vector3(
            objectDimensionsX > EPS ? newSize.x / objectDimensionsX : 1f,
            objectDimensionsY > EPS ? newSize.y / objectDimensionsY : 1f,
            objectDimensionsZ > EPS ? newSize.z / objectDimensionsZ : 1f
        );

        if (slicerController)
        {
            slicerController.Size = result;
            slicerController.RefreshSlice();
        }
    }

    [Button]
    public Vector3 CalculateScale(Vector3 newSize)
    {
        const float EPS = 1e-5f;
        return new Vector3(
            objectDimensionsX > EPS ? newSize.x / objectDimensionsX : 1f,
            objectDimensionsY > EPS ? newSize.y / objectDimensionsY : 1f,
            objectDimensionsZ > EPS ? newSize.z / objectDimensionsZ : 1f
        );
    }

    // --- Mouse Events (called by FurnitureDataInputHandler) ---

    public void OnMouseDown()
    {
        if (!enableInteraction) return;
        isMouseDown = true;
        clickDuration = 0f;
        originalPositionStored = false;
    }

    public void OnMouseUp()
    {
        if (!enableInteraction) return;
        isMouseDown = false;

        if (moving)
        {
            moving = false;
            if (col) col.enabled = true;

            Vector3Int gridPos = gridManager.GetCellPosition(transform.position, PlacementType.FreePlacedObject);
            int yRot = Mathf.RoundToInt(transform.eulerAngles.y);

            // GRID check
            bool gridIsValid = objectPlacementData.IsSpaceFree(gridPos, itemData.size, yRot, false);
            // COLLIDER check
            bool colliderIsValid = !IsOverlappingOtherColliders();

            if (gridIsValid && colliderIsValid)
            {
                objectPlacementData.AddCellObject(gameObject.GetInstanceID(), itemData.ID, gridPos, itemData.size, yRot, yRot);
            }
            else
            {
                SnapBackToOriginal();
            }
        }
        else if (clickDuration < 0.2f)
        {
            ShowFurnitureUI();
        }
        clickDuration = 0f;
    }

    public void OnMouseEnter()
    {
        if (!enableInteraction) return;
        if (rotationUI) rotationUI.SetActive(true);
    }
    public void OnMouseExit()
    {
        if (!enableInteraction) return;
        if (rotationUI) rotationUI.SetActive(false);
    }
    public void OnMouseOver() { }

    private void ShowFurnitureUI()
    {
        CheckSize();
        if (furnitureUI && !furnitureUI.gameObject.activeSelf)
        {
            var initialScale = furnitureUI.transform.localScale;
            var finalY = initialScale.y;
            initialScale.y = 0;
            furnitureUI.transform.localScale = initialScale;
            furnitureUI.transform.DOScaleY(finalY, 1f);
            furnitureUI.SetActive(true);
        }
    }

    private void Update()
    {
        if (!enableInteraction) return;

        if (isMouseDown)
        {
            clickDuration += Time.deltaTime;
            if (clickDuration > 0.35f && !moving)
            {
                moving = true;
                if (col) col.enabled = false;

                if (!originalPositionStored)
                {
                    Vector3Int gridPos = gridManager.GetCellPosition(transform.position, PlacementType.FreePlacedObject);
                    originalGridPos = gridPos;
                    originalRotation = transform.rotation;
                    objectPlacementData.RemoveCellObject(originalGridPos);
                    originalPositionStored = true;
                }
            }
        }

        if (moving && enableInteraction)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            float enter;
            Vector3 point = transform.position;
            if (plane.Raycast(ray, out enter))
            {
                point = ray.GetPoint(enter);
                Vector3Int gridPos = gridManager.GetCellPosition(point, PlacementType.FreePlacedObject);

                int yRot = Mathf.RoundToInt(transform.eulerAngles.y);
                bool gridIsValid = objectPlacementData.IsSpaceFree(gridPos, itemData.size, yRot, false);
                bool colliderIsValid = !IsOverlappingOtherColliders();

                // Snap to cell
                transform.position = gridManager.GetWorldPosition(gridPos);

                // Feedback: green if both checks pass, red if not
                if (rendererer)
                {
                    if (gridIsValid && colliderIsValid)
                        rendererer.material.color = Color.green;
                    else
                        rendererer.material.color = Color.red;
                }
            }

            // Allow rotation with scroll - but only if MousePlacementManager isn't active
            if (!IsMousePlacementManagerActive())
            {
                int scrollDirection = ScrollInputLimiter.GetScrollDirection();
                if (scrollDirection != 0)
                {
                    // Rotate by 11.25 degrees in the scroll direction
                    float newYRotation = transform.eulerAngles.y + (scrollDirection * 11.25f);
                    newYRotation = SnapToAngleIncrement(newYRotation, 11.25f);
                    transform.rotation = Quaternion.Euler(newYRotation, transform.eulerAngles.y, transform.eulerAngles.z);
                }
            }
        }
        else
        {
            if (rendererer)
                rendererer.material.color = Color.white;
        }

        HandleTouchInteraction();
    }

    private void SnapBackToOriginal()
    {
        transform.position = gridManager.GetWorldPosition(originalGridPos);
        transform.rotation = originalRotation;
        int yRot = Mathf.RoundToInt(originalRotation.eulerAngles.y);
        objectPlacementData.AddCellObject(gameObject.GetInstanceID(), itemData.ID, originalGridPos, itemData.size, yRot, yRot);
    }

    private bool IsOverlappingOtherColliders()
    {
        // Temporarily disable own collider to avoid self-detection
        if (col) col.enabled = false;

        Bounds bounds = rendererer.bounds;
        Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents * 0.95f, transform.rotation, ~0, QueryTriggerInteraction.Ignore);

        // Re-enable collider
        if (col) col.enabled = true;

        foreach (var hit in hits)
        {
            // Check against other furniture objects only
            if (hit.gameObject != gameObject && hit.gameObject.CompareTag("Furniture"))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if MousePlacementManager is currently handling input
    /// </summary>
    private bool IsMousePlacementManagerActive()
    {
        var placementManager = FindAnyObjectByType<MousePlacementManager>();
        if (placementManager == null) return false;
        
        // Consider MousePlacementManager active if it's not in Idle state
        return placementManager.state != MousePlacementManager.PlacementState.Idle;
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

    private void HandleTouchInteraction()
    {
        if (!enableInteraction) return;
        
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            
            // Check if this touch is over this furniture piece
            if (IsTouchOverFurniture(touch.position))
            {
                HandleSingleTouchOnFurniture(touch);
            }
        }
        else if (Input.touchCount == 0 && isTouchInteracting)
        {
            // Touch ended
            HandleTouchEndInteraction();
        }
    }

    private bool IsTouchOverFurniture(Vector2 touchPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform);
        }
        return false;
    }

    private void HandleSingleTouchOnFurniture(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                if (!isTouchInteracting)
                {
                    isTouchInteracting = true;
                    touchId = touch.fingerId;
                    touchStartTime = Time.time;
                    clickDuration = 0f;
                    originalPositionStored = false;
                    
                    // Show rotation UI (equivalent to OnMouseEnter)
                    if (rotationUI) rotationUI.SetActive(true);
                }
                break;
                
            case TouchPhase.Moved:
                if (isTouchInteracting && touch.fingerId == touchId)
                {
                    clickDuration = Time.time - touchStartTime;
                    
                    // Check for long press threshold
                    if (clickDuration > 0.35f && !moving)
                    {
                        // Start moving (equivalent to long press)
                        moving = true;
                        if (col) col.enabled = false;

                        if (!originalPositionStored)
                        {
                            Vector3Int gridPos = gridManager.GetCellPosition(transform.position, PlacementType.FreePlacedObject);
                            originalGridPos = gridPos;
                            originalRotation = transform.rotation;
                            objectPlacementData.RemoveCellObject(originalGridPos);
                            originalPositionStored = true;
                        }
                    }
                    
                    // Handle movement during drag
                    if (moving)
                    {
                        HandleTouchMovement(touch.position);
                    }
                }
                break;
                
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (isTouchInteracting && touch.fingerId == touchId)
                {
                    HandleTouchEndInteraction();
                }
                break;
        }
    }

    private void HandleTouchMovement(Vector2 touchPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float enter;
        
        if (plane.Raycast(ray, out enter))
        {
            Vector3 point = ray.GetPoint(enter);
            Vector3Int gridPos = gridManager.GetCellPosition(point, PlacementType.FreePlacedObject);

            int yRot = Mathf.RoundToInt(transform.eulerAngles.y);
            bool gridIsValid = objectPlacementData.IsSpaceFree(gridPos, itemData.size, yRot, false);
            bool colliderIsValid = !IsOverlappingOtherColliders();

            // Snap to cell
            transform.position = gridManager.GetWorldPosition(gridPos);

            // Visual feedback
            if (rendererer)
            {
                if (gridIsValid && colliderIsValid)
                    rendererer.material.color = Color.green;
                else
                    rendererer.material.color = Color.red;
            }
        }
    }

    private void HandleTouchEndInteraction()
    {
        if (moving)
        {
            // End movement
            moving = false;
            if (col) col.enabled = true;

            Vector3Int gridPos = gridManager.GetCellPosition(transform.position, PlacementType.FreePlacedObject);
            int yRot = Mathf.RoundToInt(transform.eulerAngles.y);

            bool gridIsValid = objectPlacementData.IsSpaceFree(gridPos, itemData.size, yRot, false);
            bool colliderIsValid = !IsOverlappingOtherColliders();

            if (gridIsValid && colliderIsValid)
            {
                objectPlacementData.AddCellObject(gameObject.GetInstanceID(), itemData.ID, gridPos, itemData.size, yRot, yRot);
            }
            else
            {
                SnapBackToOriginal();
            }
        }
        else if (clickDuration < 0.2f)
        {
            // Short tap - show furniture UI
            ShowFurnitureUI();
        }
        
        // Hide rotation UI (equivalent to OnMouseExit)
        if (rotationUI) rotationUI.SetActive(false);
        
        // Reset touch state
        isTouchInteracting = false;
        touchId = -1;
        clickDuration = 0f;
    }

}
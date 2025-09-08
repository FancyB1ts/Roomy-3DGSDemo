using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Input manager. We could ponentialy split the Raycast functionality outside of it and add the camera input ehre
/// to make it more universal and preserve Single Responsibility Rule better
/// </summary>
public class InputManager : MonoBehaviour
{
    [SerializeField]
    private Camera sceneCamera;

    private Vector3 lastPosition;

    [SerializeField]
    private LayerMask placementLayermask;

    [SerializeField]
    private LayerMask furnitureLayerMask; // Add this field in Inspector

    public bool IsOverExistingFurniture()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = sceneCamera.nearClipPlane;
        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        
        // Raycast specifically for furniture objects
        if (Physics.Raycast(ray, out hit, 100, furnitureLayerMask))
        {
            return true;
        }
        return false;
    }

    private IEnumerator CheckForCameraPan()
    {
        yield return null; // Wait one frame for furniture system to respond
        
        bool furnitureActive = IsFurnitureInteractionActive();
        bool overExistingFurniture = IsOverExistingFurniture();
        
        // Don't start camera pan if over furniture OR furniture system is active
        if (!furnitureActive && !overExistingFurniture)
        {
            isPanning = true;
            lastPanPosition = Input.mousePosition;
            OnCameraPanStarted?.Invoke();
        }
    }

    [SerializeField] 
    private MousePlacementManager mousePlacementManager;

    public event Action OnMousePressed, OnMouseReleased, OnCancle, OnUndo;

    public event Action OnCameraPanStarted, OnCameraPanFinished;
    public event Action<Vector2> OnCameraPan;

    public event Action<float> OnCameraZoom;

    private bool isPanning = false;
    private Vector3 lastPanPosition;

    public event Action<int> OnRotate;

    public event Action<bool> OnToggleDelete;

    public Vector3 GetSelectedMapPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = sceneCamera.nearClipPlane;
        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100, placementLayermask))
        {
            lastPosition = hit.point;
        }
        return lastPosition;
    }

    public bool IsInteractingWithUI()
    { 
        var isPointerOverGO=EventSystem.current.IsPointerOverGameObject();
        if (isPointerOverGO)
        {
            Debug.Log("pointer is over " + GetUIElementUnderPointer());
        }
        else
        {

        }


        return isPointerOverGO;
    }
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
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            OnCancle?.Invoke();
        if(Input.GetKeyDown(KeyCode.R))
            OnUndo?.Invoke();

       if (Input.GetMouseButtonDown(0))
        {
            if (!IsInteractingWithUI())
            {
                OnMousePressed?.Invoke(); // Furniture gets first chance
                
                // If no furniture interaction starts, begin camera pan
                StartCoroutine(CheckForCameraPan());
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            OnMouseReleased?.Invoke();
            if (isPanning)
            {
                isPanning = false;
                OnCameraPanFinished?.Invoke();
            }
        }

        if (!Mathf.Approximately(Input.mouseScrollDelta.y, 0f))
        {
            if (!IsInteractingWithUI() && !IsOverExistingFurniture())
            {
                // Only zoom camera when NOT over UI or furniture
                OnCameraZoom?.Invoke(Input.mouseScrollDelta.y);
            }
        }

        // Handle camera panning
        if (isPanning && Input.GetMouseButton(0))
        {
            Vector3 currentPos = Input.mousePosition;
            Vector2 panDelta = currentPos - lastPanPosition;
            OnCameraPan?.Invoke(panDelta);
            lastPanPosition = currentPos;
        }


        if (Input.GetKeyDown(KeyCode.Comma))
            OnRotate?.Invoke(-1);
        if (Input.GetKeyDown(KeyCode.Period))
            OnRotate?.Invoke(1);

        if (Input.GetKeyDown(KeyCode.LeftControl))
            OnToggleDelete?.Invoke(true);
        if (Input.GetKeyUp(KeyCode.LeftControl))
            OnToggleDelete?.Invoke(false);
    }

    private bool IsFurnitureInteractionActive()
    {
        return mousePlacementManager != null && 
            (mousePlacementManager.state == MousePlacementManager.PlacementState.Creating ||
                mousePlacementManager.state == MousePlacementManager.PlacementState.Moving);
    }

}

using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System;
using UnityEngine.UI;

public class CameraMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float speed = 1f;
    [SerializeField] private float movementTime = 0.1f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 15f;            // For Q/E or Mouse X
    [SerializeField] private float rotationSpeedTouch = 0.5f;       // Tweak for two-finger twist

    [Header("Zoom")]
    [SerializeField] private Vector3 zoomAmount = new Vector3(0, 0, 1f);
    [SerializeField] private Vector3 zoomLimitClose = new Vector3(0, 5, -5);
    [SerializeField] private Vector3 zoomLimitFar = new Vector3(0, 20, -20);

    [Header("Bounds")]
    [SerializeField] private int constraintXMax = 5, constraintXMin = -5;
    [SerializeField] private int constraintZMax = 5, constraintZMin = -5;

    [Header("Mouse Panning")]
    [SerializeField] private float panSpeed = 2f;
    [SerializeField] private CameraInputManager inputManager;

    [Header("Mouse Wheel Zoom")]
    [SerializeField] private float mouseWheelZoomOffset = 0f;
    [SerializeField] private float mouseWheelZoomSpeed = 2f;

    private float originalXDamping, originalYDamping, originalZDamping;
    private bool isPanning = false;

    [SerializeField] private CinemachineVirtualCamera cameraReference;
    private CinemachineTransposer cameraTransposer;

    private Vector3 newZoom;
    private Quaternion targetRotation;
    private Vector2 input;

    public Scrollbar zoomScrollbar;
     

    private void Start()
    {
        cameraTransposer = cameraReference.GetCinemachineComponent<CinemachineTransposer>();
        targetRotation = transform.rotation;
        newZoom = cameraTransposer.m_FollowOffset;

        if (zoomScrollbar != null)
        {
            zoomScrollbar.value = 0.7f; // Hardcoded middle position
            ApplyZoom(0.7f); // Make camera match this position immediately
        }

        if (inputManager != null)
        {
            inputManager.OnCameraPanStarted += StartPanning;
            inputManager.OnCameraPan += HandlePanning;
            inputManager.OnCameraPanFinished += StopPanning;
            inputManager.OnCameraZoom += HandleCameraZoom;
            inputManager.OnCameraRotate += HandleCameraRotate;
        }
    }

    void Update()
    {
        HandleInput();
        ApplyMovement();
        ApplyRotation();
        //ApplyZoom();
    }

    public void updateVerticalRotation(float delta)
    {
        targetRotation *= Quaternion.Euler(0f, delta * rotationSpeed , 0f);
    }

    public void goToTopView()
    {
        if (zoomScrollbar)
        {
            zoomScrollbar.value = 0;
        }
    }

    private void HandleCameraZoom(float scrollDelta)
    {
        if (cameraReference != null)
        {
            float zoomSpeed = 5f; // Adjust this to control zoom sensitivity
            float currentFOV = cameraReference.m_Lens.FieldOfView;
            float newFOV = currentFOV - (scrollDelta * zoomSpeed);
            
            // Clamp FOV to reasonable limits (adjust these values as needed)
            newFOV = Mathf.Clamp(newFOV, 10f, 80f);
            
            cameraReference.m_Lens.FieldOfView = newFOV;
        }
    }

    private void HandleCameraRotate(float deltaDeg)
    {
        // Apply twist rotation around Y using touch rotation speed
        targetRotation *= Quaternion.Euler(0f, deltaDeg * rotationSpeedTouch, 0f);
    }

    private void HandleInput()
    {
        // Movement input
        // input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (zoomScrollbar)
        {
            //zoomScrollbar.value -= Input.mouseScrollDelta.y*Time.deltaTime;
        }
            // ----- PC: Right-mouse drag Y-rotation -----
            //if (Input.GetMouseButton(1))
            //{
            //     float mouseDeltaX = Input.GetAxis("Mouse X");
            //   targetRotation *= Quaternion.Euler(0f, mouseDeltaX * rotationSpeed * Time.deltaTime, 0f);
            //  }

            // (Mobile two-finger twist now handled via inputManager.OnCameraRotate)

        // (Optional) Q/E keys fallback
        // int rotDir = 0;
        // if (Input.GetKey(KeyCode.Q)) rotDir = -1;
        // if (Input.GetKey(KeyCode.E)) rotDir = 1;
        // if (rotDir != 0)
        //    targetRotation *= Quaternion.Euler(0f, rotDir * rotationSpeed * Time.deltaTime, 0f);
    }

    private void ApplyMovement()
    {
        Vector3 move = (transform.forward * input.y + transform.right * input.x) * speed * Time.deltaTime;
        transform.position += move;

        // Clamp within XZ bounds
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, constraintXMin, constraintXMax),
            transform.position.y,
            Mathf.Clamp(transform.position.z, constraintZMin, constraintZMax)
        );
    }

    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime / movementTime);
    }

    public void ApplyZoom(Single delta)
    {
        Vector3 scrollbarPosition = Vector3.Lerp(
            zoomLimitClose,
            zoomLimitFar,
            1-delta
        );
        
        // Add mouse wheel offset to Z component
        scrollbarPosition.z += mouseWheelZoomOffset;
        
        cameraTransposer.m_FollowOffset = scrollbarPosition;
    }

    private Vector3 ClampVector(Vector3 value, Vector3 min, Vector3 max)
    {
        return new Vector3(
            Mathf.Clamp(value.x, min.x, max.x),
            Mathf.Clamp(value.y, min.y, max.y),
            Mathf.Clamp(value.z, min.z, max.z)
        );
    }

    private float CalculateScrollbarValueFromOffset(Vector3 currentOffset)
    {
        // Reverse the lerp calculation from ApplyZoom
        // This finds what scrollbar value would produce the current offset
        float t = Mathf.InverseLerp(zoomLimitFar.magnitude, zoomLimitClose.magnitude, currentOffset.magnitude);
        return t;
    }

    private void StartPanning()
    {
        isPanning = true;
        
        // Store original damping values
        if (cameraTransposer != null)
        {
            originalXDamping = cameraTransposer.m_XDamping;
            originalYDamping = cameraTransposer.m_YDamping; 
            originalZDamping = cameraTransposer.m_ZDamping;
            
            // Disable damping for instant panning
            cameraTransposer.m_XDamping = 0f;
            cameraTransposer.m_YDamping = 0f;
            cameraTransposer.m_ZDamping = 0f;
        }
    }

    private void HandlePanning(Vector2 panDelta)
    {
        // Convert screen pan delta to world-space movement relative to camera rotation
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        
        // Project vectors onto XZ plane (remove Y component for ground movement)
        right.y = 0;
        forward.y = 0;
        right.Normalize();
        forward.Normalize();
        
        // Apply pan movement in camera-relative directions
        Vector3 worldDelta = (-panDelta.x * right - panDelta.y * forward) * panSpeed * 0.01f;
        
        // Apply the movement to current position
        Vector3 newPosition = transform.position + worldDelta;
        
        // Apply existing constraint system
        newPosition = new Vector3(
            Mathf.Clamp(newPosition.x, constraintXMin, constraintXMax),
            newPosition.y,
            Mathf.Clamp(newPosition.z, constraintZMin, constraintZMax)
        );
        
        transform.position = newPosition;
    }

    private void StopPanning()
    {
        isPanning = false;
        
        // Restore original damping values
        if (cameraTransposer != null)
        {
            cameraTransposer.m_XDamping = originalXDamping;
            cameraTransposer.m_YDamping = originalYDamping;
            cameraTransposer.m_ZDamping = originalZDamping;
        }
    }

    private void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.OnCameraPanStarted -= StartPanning;
            inputManager.OnCameraPan -= HandlePanning;
            inputManager.OnCameraPanFinished -= StopPanning;
            inputManager.OnCameraZoom -= HandleCameraZoom;
            inputManager.OnCameraRotate -= HandleCameraRotate;
        }
    }
}
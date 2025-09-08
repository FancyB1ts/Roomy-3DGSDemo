using UnityEngine;
using TMPro;
using Slicer;

/// <summary>
/// Connects UI input fields to SlicerController for real-time dimension control.
/// Coordinate mapping: Length(X) -> Slicer.Size.x, Width(Z) -> Slicer.Size.z, Height(Y) -> Slicer.Size.y
/// </summary>
public class FurnitureDimensionController : MonoBehaviour
{
    [Header("Input Field References")]
    public TMP_InputField inputFieldLength;   // X dimension 
    public TMP_InputField inputFieldWidth;    // Z dimension
    public TMP_InputField inputFieldHeight;   // Y dimension

    [Header("Slicer Reference")]
    public SlicerController slicerController;

    // Conversion: 1 Unity unit = 100 centimeters
    private const float UNITS_TO_CM = 100f;
    private float currentHeightOffset = 0f; // Tracks Y position offset for bottom-anchoring
    private Vector3 originalMeshSize;  // Original mesh bounds in Unity units

    private bool isInitialized = false;

    private void Start()
    {
        InitializeDimensions();
        SetupInputFieldListeners();
    }

    private void OnDestroy()
    {
        RemoveInputFieldListeners();
    }

    /// <summary>
    /// Auto-detect and assign references (right-click context menu)
    /// </summary>
    [ContextMenu("Detect References")]
    public void DetectReferences()
    {
        // Find SlicerController
        if (slicerController == null)
            slicerController = GetComponent<SlicerController>();

        // Find input fields by exact names (including inactive ones)
        TMP_InputField[] inputFields = GetComponentsInChildren<TMP_InputField>(true);

        foreach (var field in inputFields)
        {
            switch (field.name)
            {
                case "InputField_Length_X":
                    inputFieldLength = field;
                    break;
                case "InputField_Width_Z":
                    inputFieldWidth = field;
                    break;
                case "InputField_Height_Y":
                    inputFieldHeight = field;
                    break;
            }
        }

        // Warn if any fields missing
        if (inputFieldLength == null) Debug.LogWarning("InputField_Length_X not found");
        if (inputFieldWidth == null) Debug.LogWarning("InputField_Width_Z not found");
        if (inputFieldHeight == null) Debug.LogWarning("InputField_Height_Y not found");
        if (slicerController == null) Debug.LogWarning("SlicerController not found");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void InitializeDimensions()
    {
        if (slicerController == null)
        {
            Debug.LogError($"SlicerController not found on {gameObject.name}");
            return;
        }

        // Get original mesh bounds to calculate real dimensions
        originalMeshSize = GetOriginalMeshSize();
        if (originalMeshSize == Vector3.zero)
        {
            Debug.LogError($"Could not determine original mesh size for {gameObject.name}");
            return;
        }

        // Reset slicer to original size (scale 1,1,1)
        slicerController.Size = Vector3.one;

        // Display actual dimensions in centimeters
        UpdateInputFieldValues(originalMeshSize);
        isInitialized = true;
    }

    private void SetupInputFieldListeners()
    {
        if (inputFieldLength != null)
            inputFieldLength.onValueChanged.AddListener(OnLengthChanged);
        if (inputFieldWidth != null)
            inputFieldWidth.onValueChanged.AddListener(OnWidthChanged);
        if (inputFieldHeight != null)
            inputFieldHeight.onValueChanged.AddListener(OnHeightChanged);
    }

    private void RemoveInputFieldListeners()
    {
        if (inputFieldLength != null)
            inputFieldLength.onValueChanged.RemoveListener(OnLengthChanged);
        if (inputFieldWidth != null)
            inputFieldWidth.onValueChanged.RemoveListener(OnWidthChanged);
        if (inputFieldHeight != null)
            inputFieldHeight.onValueChanged.RemoveListener(OnHeightChanged);
    }

    // Input handlers
    private void OnLengthChanged(string value) => UpdateDimension(0, value);  // X
    private void OnWidthChanged(string value) => UpdateDimension(2, value);   // Z
    private void OnHeightChanged(string value) => UpdateDimension(1, value);  // Y

    private void UpdateDimension(int dimensionIndex, string value)
    {
        if (!isInitialized || slicerController == null) return;

        // Handle empty or invalid input gracefully during typing
        if (string.IsNullOrEmpty(value))
        {
            // For empty input, revert to original size
            Vector3 revertSize = slicerController.Size;
            revertSize[dimensionIndex] = 1f;
            slicerController.Size = revertSize;

            // Reset offset when reverting
            if (dimensionIndex == 1) // Y dimension (height)
            {
                slicerController.Offset = Vector3.zero;
            }

            slicerController.RefreshSlice();
            return;
        }

        if (float.TryParse(value, out float newValueCm) && newValueCm > 0)
        {
            // Convert from centimeters to Unity units
            float newValueUnits = newValueCm / UNITS_TO_CM;

            // Calculate scale factor relative to original mesh size
            float scaleFactor = newValueUnits / originalMeshSize[dimensionIndex];

            // Apply scale to slicer
            Vector3 newSize = slicerController.Size;
            newSize[dimensionIndex] = scaleFactor;
            slicerController.Size = newSize;

            // Handle bottom-anchoring for height changes using position adjustment
            if (dimensionIndex == 1) // Y dimension (height)
            {
                // Calculate how much the mesh center will move due to scaling
                float heightDifference = (scaleFactor - 1f) * originalMeshSize.y;
                currentHeightOffset = heightDifference / 2f;

                // Adjust position to keep bottom at ground level
                Vector3 currentPos = transform.position;
                currentPos.y = currentHeightOffset;
                transform.position = currentPos;

                // Reset slicer offset since we're using position adjustment
                slicerController.Offset = Vector3.zero;
            }

            slicerController.RefreshSlice();
        }
        // If parse fails or value <= 0, do nothing (keeps last valid value)
    }

    private void UpdateInputFieldValues(Vector3 meshSizeInUnits)
    {
        // Convert mesh size from Unity units to centimeters for display
        if (inputFieldLength != null)
            inputFieldLength.text = (meshSizeInUnits.x * UNITS_TO_CM).ToString("F0");
        if (inputFieldWidth != null)
            inputFieldWidth.text = (meshSizeInUnits.z * UNITS_TO_CM).ToString("F0");
        if (inputFieldHeight != null)
            inputFieldHeight.text = (meshSizeInUnits.y * UNITS_TO_CM).ToString("F0");
    }

    private Vector3 GetOriginalMeshSize()
    {
        // Get mesh bounds from MeshRenderer or MeshFilter
        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            return meshRenderer.bounds.size;
        }

        var meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return meshFilter.sharedMesh.bounds.size;
        }

        Debug.LogWarning($"No MeshRenderer or MeshFilter found on {gameObject.name}");
        return Vector3.zero;
    }

    /// <summary>
    /// Gets the current height offset needed to keep furniture bottom-anchored
    /// </summary>
    public float GetHeightOffset()
    {
        return currentHeightOffset;
    }

}
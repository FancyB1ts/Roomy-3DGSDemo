using UnityEngine;
using UnityEngine.UI;
using Slicer; // Add this import

public class ButtonRotateFurniture : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationAmount = 22.5f; // 2 steps of 11.25°
    
    [Header("Button Reference")]
    [SerializeField] private Button rotateButton;
    
    public GameObject targetFurniture; // Set this externally when furniture is selected
    
    void Start()
    {
        if (rotateButton != null)
            rotateButton.onClick.AddListener(() => RotateFurniture(rotationAmount));
    }
    
    public void RotateFurniture(float degrees)
    {
        if (targetFurniture == null) return;
        
        // Find the furniture root with SlicerController for rotation
        GameObject rotationTarget = targetFurniture;
        SlicerController slicerController = rotationTarget.GetComponent<SlicerController>();

        // If targetFurniture doesn't have SlicerController, find the parent that does
        if (slicerController == null)
        {
            Transform current = rotationTarget.transform;
            while (current.parent != null && slicerController == null)
            {
                current = current.parent;
                slicerController = current.GetComponent<SlicerController>();
                if (slicerController != null)
                {
                    rotationTarget = current.gameObject;
                }
            }
        }

        // Rotate the object with SlicerController
        float newY = rotationTarget.transform.eulerAngles.y + degrees;
        rotationTarget.transform.rotation = Quaternion.Euler(0f, newY, 0f);
    }
}
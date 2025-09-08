using UnityEngine;
using LanguageLocalization; 

public class FurnitureUIController : MonoBehaviour
{
    public GameObject RotationUI;
    public GameObject MovementUI;
    public GameObject DataUI;

    [SerializeField] private GameObject furnitureRoot;

    public bool isDataUIOpen => DataUI != null && DataUI.activeSelf;
    public bool IsRotationUIVisible => RotationUI != null && RotationUI.activeSelf;
    public void EnableRotationUI() => RotationUI?.SetActive(true);
    public void DisableRotationUI() => RotationUI?.SetActive(false);
    public void EnableMovementUI() => MovementUI?.SetActive(true);
    public void DisableMovementUI() => MovementUI?.SetActive(false);

    public void EnableDataUI(Vector3 screenPosition)
    {
        DataUI?.SetActive(true);
        
        if (DataUI != null)
        {
            // Get the anchor button and content box
            Transform anchorButton = DataUI.transform.Find("PF_UI_Button_Icon-Confirm");
            Transform contentBox = DataUI.transform.Find("ContentBox");
            
            if (anchorButton != null)
            {
                // Position anchor button at exact click position
                RectTransform anchorRect = anchorButton.GetComponent<RectTransform>();
                if (anchorRect != null)
                {
                    anchorRect.position = screenPosition;
                }
            }
            
            if (contentBox != null)
            {
                // Calculate smart horizontal offset for content box
                RectTransform contentRect = contentBox.GetComponent<RectTransform>();
                if (contentRect != null)
                {
                    Vector3 smartPosition = CalculateHorizontalSmartOffset(screenPosition, contentRect);
                    contentRect.position = smartPosition;
                }
            }
        }

        // Apply localization after UI is positioned and active
        StartCoroutine(ApplyLocalizationToActiveUI());
    }

    public void EnableDataUI() => DataUI?.SetActive(true);
    public void DisableDataUI() => DataUI?.SetActive(false);

    private Vector3 CalculateHorizontalSmartOffset(Vector3 clickPosition, RectTransform contentRect)
    {
        // Get canvas bounds
        Canvas canvas = contentRect.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        
        float boxWidth = contentRect.rect.width;
        float canvasWidth = canvasRect.rect.width;
        
        // Convert click to local canvas coordinates
        Vector2 localClickPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, clickPosition, canvas.worldCamera, out localClickPos);
        
        // Use prefab's natural horizontal offset (+120 to the right)
        float naturalX = localClickPos.x + 120f;
        
        // Check if natural position would cause clipping
        float halfBoxWidth = boxWidth * 0.5f;
        float halfCanvasWidth = canvasWidth * 0.5f;
        
        // Clamp only if natural position would clip
        float finalX = Mathf.Clamp(naturalX, -halfCanvasWidth + halfBoxWidth, halfCanvasWidth - halfBoxWidth);
        
        // Use prefab's vertical offset (+65 above click)
        Vector2 finalLocalPos = new Vector2(finalX, localClickPos.y + 65f);
        
        // Convert back to world position
        Vector3 worldPos = canvasRect.TransformPoint(finalLocalPos);
        return worldPos;
    }


        // Deletes the entire furniture instance
        public void DeleteFurniture()
        {
            // Close the UI first
            DisableDataUI();
            DisableRotationUI();
            DisableMovementUI();
            
            // Notify MousePlacementManager that this object is being deleted
            var placementManager = FindObjectOfType<MousePlacementManager>();
            if (placementManager != null)
            {
                placementManager.OnFurnitureDeleted(furnitureRoot != null ? furnitureRoot : gameObject);
            }
            
            // Destroy the furniture root (or fallback to current object)
            if (furnitureRoot != null)
            {
                Destroy(furnitureRoot);
            }
            else
            {
                Destroy(gameObject);
            }
        }

    private System.Collections.IEnumerator ApplyLocalizationToActiveUI()
    {
        yield return null; // Wait one frame for UI to be fully active
        
        var localizationSource = FindObjectOfType<Localization_SOURCE>();
        if (localizationSource != null)
        {
            Debug.Log("Applying localization to active furniture UI");
            localizationSource.RefreshTextElementsAndKeys();
            localizationSource.LoadLanguage(localizationSource.selectedLanguage);
        }
    }

}

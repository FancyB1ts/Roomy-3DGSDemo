using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FloorplanScaler : MonoBehaviour
{
    [Header("UI References")]
    public GameObject areaInputUI;
    public TMP_InputField areaInputField;
    public Button calculateButton;
    public TMP_Dropdown unitDropdown;

    [Header("Scale Application")]
    public Transform floorplanPlane;

    [Header("UV Calculation")]
    public RectTransform imageContainer;

    private float currentPolygonAreaPixels = 0f;
    private float totalImageAreaPixels = 0f;

    /// <summary>
    /// Recompute the image container area once layout/texture is ready.
    /// Safe to call multiple times.
    /// </summary>
    public void RecomputeImageArea()
    {
        if (imageContainer == null)
        {
            // try to auto-find again if needed
            RawImage rawImg = GetComponentInParent<RawImage>();
            if (rawImg != null) imageContainer = rawImg.rectTransform;
            else
            {
                Image img = GetComponentInParent<Image>();
                if (img != null) imageContainer = img.rectTransform;
            }
        }

        if (imageContainer != null)
        {
            Rect imageRect = imageContainer.rect;
            totalImageAreaPixels = imageRect.width * imageRect.height;
            Debug.Log($"[FloorplanScaler] Recomputed image area: {totalImageAreaPixels:F2} px² ({imageRect.width:F1} x {imageRect.height:F1})");
        }
        else
        {
            Debug.LogError("[FloorplanScaler] RecomputeImageArea failed — imageContainer not found");
        }
    }

    private enum UnitType { Jo, SquareMeters, SquareFeet }
    private UnitType currentUnit = UnitType.Jo;

    void Start()
    {
        Debug.Log("[FloorplanScaler] Start()");

        if (areaInputUI != null)
            areaInputUI.SetActive(false);

        if (calculateButton == null && areaInputUI != null)
        {
            calculateButton = areaInputUI.GetComponentInChildren<Button>();
            if (calculateButton != null)
            {
                Debug.Log("[FloorplanScaler] Auto-found calculate button");
            }
        }

        if (calculateButton != null)
        {
            calculateButton.onClick.RemoveAllListeners();
            calculateButton.onClick.AddListener(CalculateAndApplyScale);
            Debug.Log("[FloorplanScaler] Button wired to CalculateAndApplyScale");
        }
        else
        {
            Debug.LogWarning("[FloorplanScaler] Calculate button not found! Scale calculation won't work.");
        }

        if (unitDropdown != null)
        {
            unitDropdown.value = 0; // Default to Jo
            unitDropdown.onValueChanged.AddListener(OnUnitDropdownChanged);
            OnUnitDropdownChanged(unitDropdown.value);
        }

        if (imageContainer == null)
        {
            RawImage rawImg = GetComponentInParent<RawImage>();
            if (rawImg != null)
            {
                imageContainer = rawImg.rectTransform;
                Debug.Log("[FloorplanScaler] Auto-found image container from RawImage");
            }
            else
            {
                Image img = GetComponentInParent<Image>();
                if (img != null)
                {
                    imageContainer = img.rectTransform;
                    Debug.Log("[FloorplanScaler] Auto-found image container from Image");
                }
            }
        }

        if (imageContainer != null)
        {
            Rect imageRect = imageContainer.rect;
            totalImageAreaPixels = imageRect.width * imageRect.height;
            Debug.Log($"[FloorplanScaler] Total image area: {totalImageAreaPixels:F2} px ({imageRect.width:F1} x {imageRect.height:F1})");
        }
        else
        {
            Debug.LogError("[FloorplanScaler] imageContainer not found! UV calculation will not work.");
        }

        if (floorplanPlane == null)
        {
            Debug.LogWarning("[FloorplanScaler] floorplanPlane not assigned! Trying to auto-find.");

            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("floorplan") || obj.name.ToLower().Contains("ground"))
                {
                    if (obj.GetComponent<Renderer>() != null)
                    {
                        floorplanPlane = obj.transform;
                        Debug.Log($"[FloorplanScaler] Auto-found plane: {floorplanPlane.name}");
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.Log($"[FloorplanScaler] Plane reference set to: {floorplanPlane.name}");
        }
    }

    public void OnUnitDropdownChanged(int value)
    {
        currentUnit = (UnitType)value;
        Debug.Log($"[FloorplanScaler] Unit changed to: {currentUnit}");
    }

    public void SetPolygonArea(float pixelArea)
    {
        currentPolygonAreaPixels = pixelArea;
        Debug.Log($"[FloorplanScaler] SetPolygonArea: {pixelArea:F2} px²");

        if (totalImageAreaPixels <= 0f)
        {
            RecomputeImageArea();
        }

        if (totalImageAreaPixels > 0)
        {
            float uvAreaRatio = currentPolygonAreaPixels / totalImageAreaPixels;
            Debug.Log($"[FloorplanScaler] UV ratio: {uvAreaRatio:F6} ({uvAreaRatio * 100:F2}%)");
        }
        else
        {
            Debug.LogError("[FloorplanScaler] Cannot calculate UV ratio — total image area is 0");
        }

        if (areaInputUI != null)
        {
            areaInputUI.SetActive(true);
        }
        else
        {
            Debug.LogError("[FloorplanScaler] areaInputUI is null — cannot show input");
        }
    }

    public void ResetScale()
    {
        Debug.Log("[FloorplanScaler] ResetScale()");
        currentPolygonAreaPixels = 0f;
        if (areaInputUI != null)
        {
            areaInputUI.SetActive(false);
        }
    }

    public void CalculateAndApplyScale()
    {
        Debug.Log("[FloorplanScaler] CalculateAndApplyScale()");

        if (currentPolygonAreaPixels <= 0f)
        {
            Debug.LogWarning("[FloorplanScaler] Cannot scale — polygon area is 0");
            return;
        }

        if (totalImageAreaPixels <= 0f)
        {
            RecomputeImageArea();
            if (totalImageAreaPixels <= 0f)
            {
                Debug.LogError("[FloorplanScaler] Cannot scale — total image area is 0");
                return;
            }
        }

        if (areaInputField == null)
        {
            Debug.LogError("[FloorplanScaler] areaInputField is null");
            return;
        }

        string inputText = areaInputField.text;
        Debug.Log($"[FloorplanScaler] User input: '{inputText}'");

        if (string.IsNullOrEmpty(inputText)) return;
        if (!float.TryParse(inputText, out float realWorldArea)) return;
        if (realWorldArea <= 0) return;

        float areaInSquareMeters = ConvertToSquareMeters(realWorldArea);
        float scaleFactor = CalculateUVBasedScale(currentPolygonAreaPixels, totalImageAreaPixels, areaInSquareMeters);
        Debug.Log($"[FloorplanScaler] Calculated scaleFactor: {scaleFactor:F4}");

        ApplyScaleToPlane(scaleFactor);
    }

    private float ConvertToSquareMeters(float value)
    {
        switch (currentUnit)
        {
            case UnitType.Jo:
                return value * 1.62f; // 1 畳 ≈ 1.62 m²
            case UnitType.SquareFeet:
                return value * 0.092903f; // 1 ft² ≈ 0.092903 m²
            default:
                return value;
        }
    }

    private float CalculateUVBasedScale(float polygonAreaPixels, float totalImageAreaPixels, float realWorldAreaSquareMeters)
    {
        float uvAreaRatio = polygonAreaPixels / totalImageAreaPixels;
        float totalFloorplanAreaSquareMeters = realWorldAreaSquareMeters / uvAreaRatio;
        Debug.Log($"[FloorplanScaler] Total floorplan area estimate: {totalFloorplanAreaSquareMeters:F2} m²");
        float scaleFactor = Mathf.Sqrt(totalFloorplanAreaSquareMeters) / 10f; // adjust for Unity 10x10 plane
        return scaleFactor;
    }

    private void ApplyScaleToPlane(float scaleFactor)
    {
        if (floorplanPlane == null)
        {
            Debug.LogError("[FloorplanScaler] Cannot apply scale — floorplanPlane is null");
            return;
        }
        Vector3 newScale = new Vector3(scaleFactor, 1f, scaleFactor);
        floorplanPlane.localScale = newScale;
        Debug.Log($"[FloorplanScaler] ✅ Applied scale: {scaleFactor:F4} → plane scale: {newScale}");
    }
}

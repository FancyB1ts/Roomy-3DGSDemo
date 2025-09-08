using UnityEngine;
using UnityEngine.UI;

public class ButtonHighlighter : MonoBehaviour
{
    [Header("Button References")]
    public Button outlineButton;        // Button 1: Active during outlining
    public Button dimensionButton;      // Button 2: Active during dimension input
    
    [Header("Component References")]
    public PolygonDrawer polygonDrawer;
    public FloorplanScaler floorplanScaler;
    
    void Start()
    {
        // Auto-find components if not assigned
        if (polygonDrawer == null)
            polygonDrawer = FindObjectOfType<PolygonDrawer>();
        
        if (floorplanScaler == null)
            floorplanScaler = FindObjectOfType<FloorplanScaler>();
        
        // Start checking states
        InvokeRepeating(nameof(CheckStates), 0.1f, 0.1f);
    }
    
    void CheckStates()
    {
        // State 1: Outlining is active but polygon is not complete
        bool shouldHighlightOutline = IsOutlining() && !IsPolygonComplete();
        
        // State 2: Polygon is complete and dimension UI is active
        bool shouldHighlightDimension = IsPolygonComplete() && IsDimensionUIActive();
        
        // Apply selection state to trigger highlighted appearance
        SetButtonSelected(outlineButton, shouldHighlightOutline);
        SetButtonSelected(dimensionButton, shouldHighlightDimension);
    }
    
    private bool IsOutlining()
    {
        if (polygonDrawer == null) return false;
        return polygonDrawer.lineRenderer != null && polygonDrawer.lineRenderer.enabled;
    }
    
    private bool IsPolygonComplete()
    {
        if (polygonDrawer == null) return false;
        return polygonDrawer.IsComplete();
    }
    
    private bool IsDimensionUIActive()
    {
        if (floorplanScaler == null) return false;
        return floorplanScaler.areaInputUI != null && floorplanScaler.areaInputUI.activeSelf;
    }
    
    private void SetButtonSelected(Button button, bool selected)
    {
        if (button == null) return;
        
        // Use Unity's built-in selection system to trigger highlighted state
        if (selected && button.targetGraphic != null)
        {
            button.targetGraphic.CrossFadeColor(button.colors.highlightedColor, 0.1f, true, true);
        }
        else if (!selected && button.targetGraphic != null)
        {
            button.targetGraphic.CrossFadeColor(button.colors.normalColor, 0.1f, true, true);
        }
    }
}
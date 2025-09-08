using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Radishmouse;

public class FloorplanOutliner : MonoBehaviour, IPointerClickHandler
{
    [Header("Debug Controls")]
    public bool activateOnStart = false;
    
    [Header("Visual Settings")]
    public Color drawingColor = Color.red;
    public Color completeColor = Color.blue;
    public Color highlightColor = Color.cyan;
    public float lineWidth = 3f;  // Reduced from 10 to 3
    public float pointRadius = 8f;
    public float closeThreshold = 12f;
    
    [Header("Required Components")]
    public UILineRenderer lineRenderer;
    public Transform pointsContainer;
    
    // Private variables
    private RawImage floorplanImage;
    private List<Vector2> points = new List<Vector2>(); // Simple local coordinates
    private List<GameObject> pointObjects = new List<GameObject>();
    private bool isDrawing = false;
    private bool isComplete = false;
    private GameObject firstPointHighlight;
    
    void Start()
    {
        // Get RawImage on same GameObject
        floorplanImage = GetComponent<RawImage>();
        RectTransform rawImageRect = GetComponent<RectTransform>();
        
        // Force children to EXACTLY match parent anchors and position
        if (lineRenderer != null)
        {
            RectTransform lineRect = lineRenderer.rectTransform;
            lineRect.anchoredPosition = Vector2.zero;
            lineRect.anchorMin = rawImageRect.anchorMin;  // Match parent anchors
            lineRect.anchorMax = rawImageRect.anchorMax;  // Match parent anchors
            lineRect.sizeDelta = Vector2.zero;
            lineRect.pivot = rawImageRect.pivot;          // Match parent pivot
        }
        
        if (pointsContainer != null)
        {
            RectTransform pointsRect = pointsContainer.GetComponent<RectTransform>();
            pointsRect.anchoredPosition = Vector2.zero;
            pointsRect.anchorMin = rawImageRect.anchorMin;  // Match parent anchors
            pointsRect.anchorMax = rawImageRect.anchorMax;  // Match parent anchors
            pointsRect.sizeDelta = Vector2.zero;
            pointsRect.pivot = rawImageRect.pivot;          // Match parent pivot
        }
        
        // Setup line renderer
        lineRenderer.color = drawingColor;
        lineRenderer.thickness = lineWidth;
        lineRenderer.closedPolygon = false;
        lineRenderer.smoothCorners = false;
        
        if (activateOnStart)
            StartOutlining();
        else
            SetActive(false);
    }
    
    public void StartOutlining()
    {
        Reset();
        SetActive(true);
        isDrawing = true;
        
        Debug.Log("Click to place points. Click first point when you have 3+ to close.");
    }
    
    public void StopOutlining()
    {
        SetActive(false);
        isDrawing = false;
    }
    
    public void Reset()
    {
        // Clear all data
        points.Clear();
        isComplete = false;
        
        // Clear points
        foreach (GameObject point in pointObjects)
            if (point != null) DestroyImmediate(point);
        pointObjects.Clear();
        
        // Properly clear line renderer
        if (lineRenderer != null)
        {
            lineRenderer.points = new Vector2[0];
            lineRenderer.closedPolygon = false;
            lineRenderer.smoothCorners = false;
            lineRenderer.color = drawingColor;
            lineRenderer.MarkMeshDirty();
        }
        
        // Clear highlight
        if (firstPointHighlight != null)
        {
            DestroyImmediate(firstPointHighlight);
            firstPointHighlight = null;
        }
        
        // IMPORTANT: Restart drawing mode
        isDrawing = true;
        SetActive(true);
        
        Debug.Log("Reset complete - ready for new outline");
    }
    
    private void SetActive(bool active)
    {
        lineRenderer.enabled = active;
        pointsContainer.gameObject.SetActive(active);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isDrawing || isComplete) return;
        
        // Get click position in LOCAL coordinates (same as line renderer)
        Vector2 localPoint;
        RectTransform rect = GetComponent<RectTransform>();
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            // Check if clicking first point to close
            if (points.Count >= 3 && IsNearFirstPoint(localPoint))
            {
                ClosePolygon();
            }
            else
            {
                AddPoint(localPoint);
            }
        }
    }
    
    private bool IsNearFirstPoint(Vector2 clickPoint)
    {
        if (points.Count < 3) return false;
        return Vector2.Distance(clickPoint, points[0]) <= closeThreshold;
    }
    
    private void AddPoint(Vector2 localPoint)
    {
        points.Add(localPoint);
        CreatePointObject(localPoint);
        UpdateLine();
        
        if (points.Count >= 3)
            ShowFirstPointHighlight();
        
        Debug.Log($"Point {points.Count} added at {localPoint}");
    }
    
    private void CreatePointObject(Vector2 localPoint)
    {
        GameObject point = new GameObject($"Point_{points.Count - 1}");
        point.transform.SetParent(pointsContainer);
        
        Image img = point.AddComponent<Image>();
        img.color = drawingColor;
        img.rectTransform.sizeDelta = new Vector2(pointRadius * 2, pointRadius * 2);
        img.rectTransform.anchoredPosition = localPoint;
        
        // Use Unity's built-in round sprite
        img.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        
        pointObjects.Add(point);
    }
    
    private void UpdateLine()
    {
        if (points.Count < 2)
        {
            lineRenderer.points = new Vector2[0];
            return;
        }
        
        // Direct assignment - no coordinate conversion needed
        lineRenderer.points = points.ToArray();
        lineRenderer.MarkMeshDirty();
    }
    
    private void ShowFirstPointHighlight()
    {
        if (firstPointHighlight != null) return;
        
        firstPointHighlight = new GameObject("FirstPointHighlight");
        firstPointHighlight.transform.SetParent(pointsContainer);
        
        Image img = firstPointHighlight.AddComponent<Image>();
        img.color = highlightColor;
        img.rectTransform.sizeDelta = new Vector2(pointRadius * 3, pointRadius * 3);
        img.rectTransform.anchoredPosition = points[0];
        
        // Use Unity's built-in round sprite for highlight too
        img.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        
        StartCoroutine(PulseHighlight());
    }
    
    private System.Collections.IEnumerator PulseHighlight()
    {
        Image img = firstPointHighlight.GetComponent<Image>();
        float time = 0f;
        
        while (firstPointHighlight != null && !isComplete)
        {
            time += Time.deltaTime;
            Color c = img.color;
            c.a = 0.3f + 0.4f * Mathf.Sin(time * 3f);
            img.color = c;
            yield return null;
        }
    }
    
    private void ClosePolygon()
    {
        if (points.Count < 3) return;
        
        isComplete = true;
        isDrawing = false;
        
        // Remove highlight
        if (firstPointHighlight != null)
        {
            DestroyImmediate(firstPointHighlight);
            firstPointHighlight = null;
        }
        
        // Close the line by adding first point at the end
        List<Vector2> closedPoints = new List<Vector2>(points);
        closedPoints.Add(points[0]);
        
        lineRenderer.points = closedPoints.ToArray();
        lineRenderer.closedPolygon = true;
        lineRenderer.smoothCorners = true;
        lineRenderer.color = completeColor;
        lineRenderer.MarkMeshDirty();
        
        // Update point colors
        foreach (GameObject point in pointObjects)
        {
            Image img = point.GetComponent<Image>();
            if (img != null) img.color = completeColor;
        }
        
        float area = CalculateArea();
        Debug.Log($"Polygon complete! {points.Count} points, Area: {area:F2}");
    }
    
    private float CalculateArea()
    {
        if (points.Count < 3) return 0f;
        
        float area = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            int j = (i + 1) % points.Count;
            area += points[i].x * points[j].y - points[j].x * points[i].y;
        }
        return Mathf.Abs(area) / 2f;
    }
    
    // Public accessors
    public bool IsComplete() => isComplete;
    public List<Vector2> GetPoints() => new List<Vector2>(points);
    public float GetArea() => CalculateArea();
    
    // Convert to UV coordinates for external use (e.g., scaling calculations)
    public List<Vector2> GetPointsUV()
    {
        List<Vector2> uvPoints = new List<Vector2>();
        RectTransform rect = GetComponent<RectTransform>();
        Rect rectBounds = rect.rect;
        
        foreach (Vector2 point in points)
        {
            float u = (point.x - rectBounds.x) / rectBounds.width;
            float v = (point.y - rectBounds.y) / rectBounds.height;
            uvPoints.Add(new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v)));
        }
        
        return uvPoints;
    }
    
    [ContextMenu("Start Outlining")]
    public void StartOutlining_Inspector() => StartOutlining();
    
    [ContextMenu("Reset")]
    public void Reset_Inspector() => Reset();
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Radishmouse;

public class PolygonDrawer : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Debug Controls")]
    public bool activateOnStart = false;
    
    [Header("Visual Settings")]
    public Color drawingColor = Color.red;
    public Color completeColor = Color.blue;
    public float lineWidth = 3f;
    public float closeThreshold = 12f;
    
    [Header("Point Visual Settings")]
    public Sprite firstPointSprite;
    public Sprite regularPointSprite;
    public float pointSize = 16f;
    
    [Header("Required Components")]
    public UILineRenderer lineRenderer;
    public Transform pointsContainer;
    
    // Auto-find other components
    private PolygonRenderer polygonRenderer;
    private FloorplanScaler floorplanScaler;
    
    // Core state
    private List<Vector2> points = new List<Vector2>();
    private List<GameObject> pointObjects = new List<GameObject>();
    private bool isDrawing = false;
    private bool isComplete = false;
    
    // Dragging
    private bool isDragging = false;
    private int draggedPointIndex = -1;
    private Vector2 dragOffset;
    
    void Start()
    {
        SetupLineRenderer();
        
        // Auto-find other components
        polygonRenderer = GetComponent<PolygonRenderer>();
        floorplanScaler = GetComponent<FloorplanScaler>();
        
        if (activateOnStart)
            StartOutlining();
        else
            SetActive(false);
    }
    
    private void SetupRectTransform(RectTransform rect)
    {
        if (rect == null) return;
        RectTransform parent = GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.anchorMin = parent.anchorMin;
        rect.anchorMax = parent.anchorMax;
        rect.sizeDelta = Vector2.zero;
        rect.pivot = parent.pivot;
    }

    private bool rectTransformSetupComplete = false;
    private void EnsureRectTransformSetup()
    {
        if (rectTransformSetupComplete) return;
        
        SetupRectTransform(lineRenderer?.rectTransform);
        SetupRectTransform(pointsContainer?.GetComponent<RectTransform>());
        rectTransformSetupComplete = true;
        
        Debug.Log("RectTransform setup completed on demand");
    }
    
    private void SetupLineRenderer()
    {
        // Auto-find UILineRenderer if not assigned
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<UILineRenderer>();
        }
        
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer is null! Outliner cannot work without line renderer.");
            return;
        }
        
        lineRenderer.color = drawingColor;
        lineRenderer.thickness = lineWidth;
        lineRenderer.closedPolygon = false;
        lineRenderer.smoothCorners = false;
    }
    
    public void StartOutlining()
    {
        EnsureRectTransformSetup();
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
        points.Clear();
        isComplete = false;
        
        foreach (GameObject point in pointObjects)
            if (point != null) DestroyImmediate(point);
        pointObjects.Clear();
        
        if (lineRenderer != null)
        {
            lineRenderer.points = new Vector2[0];
            lineRenderer.closedPolygon = false;
            lineRenderer.smoothCorners = false;
            lineRenderer.color = drawingColor;
            lineRenderer.MarkMeshDirty();
        }
        
        // Notify other components
        if (polygonRenderer != null)
            polygonRenderer.Clear();
        
        if (floorplanScaler != null)
            floorplanScaler.ResetScale();
        
        isDrawing = true;
        SetActive(true);
    }
    
    private void SetActive(bool active)
    {
        if (lineRenderer != null)
            lineRenderer.enabled = active;
        
        if (pointsContainer != null)
            pointsContainer.gameObject.SetActive(active);
    }
    
    private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
    {
        RectTransform rect = GetComponent<RectTransform>();
        Camera cam = eventData.pressEventCamera;
        
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, cam, out localPoint);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        
        if (!isDrawing || isComplete) 
            return;
        
        if (!TryGetLocalPoint(eventData, out Vector2 localPoint)) 
            return;
        
        if (points.Count >= 3 && IsNearFirstPoint(localPoint))
        {
            ClosePolygon();
        }
        else
        {
            AddPoint(localPoint);
        }
    }
    
    private void ClosePolygon()
    {
        if (points.Count < 3) return;
        
        isComplete = true;
        isDrawing = false;
        
        Debug.Log("=== POLYGON COMPLETED ===");
        Debug.Log($"Points count: {points.Count}");
        
        // Check for self-intersections and warn user
        if (HasSelfIntersection())
        {
            Debug.Log("⚠️ Avoid self intersections for an accurate scale calculation");
        }
        
        // Update main line renderer
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
            if (img != null) 
            {
                img.color = completeColor;
                if (regularPointSprite != null)
                {
                    img.sprite = regularPointSprite;
                }
            }
        }
        
        float area = CalculatePolygonArea();
        Debug.Log($"Calculated area: {area:F2} square units");
        
        // Notify other components
        if (polygonRenderer != null)
        {
            Debug.Log("Notifying PolygonRenderer...");
            polygonRenderer.UpdatePolygon(new List<Vector2>(points), true);
        }
        else
        {
            Debug.LogWarning("PolygonRenderer is null - not found on same GameObject");
        }
        
        if (floorplanScaler != null)
        {
            Debug.Log($"Notifying FloorplanScaler with area: {area:F2}");
            floorplanScaler.SetPolygonArea(area);
        }
        else
        {
            Debug.LogWarning("FloorplanScaler is null - not found on same GameObject");
        }
        
        Debug.Log("=== POLYGON COMPLETION FINISHED ===");
    }
    
    private void AddPoint(Vector2 localPoint)
    {
        points.Add(localPoint);
        
        GameObject point = new GameObject($"Point_{points.Count - 1}");
        point.transform.SetParent(pointsContainer);
        
        Image img = point.AddComponent<Image>();
        img.color = drawingColor;
        
        // FIX: Apply canvas scale normalization to point size
        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        img.rectTransform.sizeDelta = new Vector2(pointSize, pointSize) * scaleFactor;
        img.rectTransform.anchoredPosition = localPoint;
        
        bool isFirst = (points.Count == 1);
        Sprite sprite = isFirst ? firstPointSprite : regularPointSprite;
        
        if (sprite != null)
        {
            img.sprite = sprite;
        }
        else
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillAmount = 1f;
        }
        
        pointObjects.Add(point);
        UpdateLine();
    }
    
    private void UpdateLine()
    {
        Vector2[] linePoints = points.Count >= 2 ? points.ToArray() : new Vector2[0];
        lineRenderer.points = linePoints;
        lineRenderer.MarkMeshDirty();
    }
    
    private bool IsNearFirstPoint(Vector2 clickPoint)
    {
        if (points.Count < 3) return false;
        
        // FIX: Apply canvas scale normalization to close threshold
        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        float normalizedThreshold = closeThreshold * scaleFactor;
        
        return Vector2.Distance(clickPoint, points[0]) <= normalizedThreshold;
    }
    
    // Drag functionality
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isComplete) return;
        
        if (!TryGetLocalPoint(eventData, out Vector2 localPoint)) return;
        
        draggedPointIndex = FindNearestPointIndex(localPoint);
        
        if (draggedPointIndex >= 0)
        {
            isDragging = true;
            dragOffset = localPoint - points[draggedPointIndex];
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || draggedPointIndex < 0) return;
        
        if (!TryGetLocalPoint(eventData, out Vector2 localPoint)) return;
        
        Vector2 newPosition = localPoint - dragOffset;
        points[draggedPointIndex] = newPosition;
        
        if (pointObjects[draggedPointIndex] != null)
        {
            pointObjects[draggedPointIndex].GetComponent<RectTransform>().anchoredPosition = newPosition;
        }
        
        UpdateLineForClosedPolygon();
        
        // Recalculate area and notify
        float area = CalculatePolygonArea();
        
        if (polygonRenderer != null)
            polygonRenderer.UpdatePolygon(new List<Vector2>(points), true);
        
        if (floorplanScaler != null)
            floorplanScaler.SetPolygonArea(area);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        isDragging = false;
        draggedPointIndex = -1;
        
        // Check for self-intersections after dragging
        if (HasSelfIntersection())
        {
            Debug.Log("⚠️ Avoid self intersections for an accurate scale calculation");
        }
        else
        {
            Debug.Log("✅ Good! No overlapping lines detected.");
        }
    }
    
    private int FindNearestPointIndex(Vector2 localPoint)
    {
        float nearestDistance = float.MaxValue;
        int nearestIndex = -1;
        
        // FIX: Apply canvas scale normalization to drag threshold
        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        float dragThreshold = (pointSize * 0.75f) * scaleFactor;
        
        for (int i = 0; i < points.Count; i++)
        {
            float distance = Vector2.Distance(localPoint, points[i]);
            if (distance < dragThreshold && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }
        
        return nearestIndex;
    }
    
    private void UpdateLineForClosedPolygon()
    {
        if (!isComplete) return;
        
        List<Vector2> closedPoints = new List<Vector2>(points);
        closedPoints.Add(points[0]);
        
        lineRenderer.points = closedPoints.ToArray();
        lineRenderer.MarkMeshDirty();
    }
    
    private float CalculatePolygonArea()
    {
        if (points.Count < 3) return 0f;
        
        // Shoelace formula for polygon area
        float area = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            int j = (i + 1) % points.Count;
            area += points[i].x * points[j].y - points[j].x * points[i].y;
        }
        
        return Mathf.Abs(area) / 2f;
    }
    
    private bool HasSelfIntersection()
    {
        if (points.Count < 4) return false;
        
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 line1Start = points[i];
            Vector2 line1End = points[(i + 1) % points.Count];
            
            for (int j = i + 2; j < points.Count; j++)
            {
                if (j == points.Count - 1 && i == 0) continue;
                
                Vector2 line2Start = points[j];
                Vector2 line2End = points[(j + 1) % points.Count];
                
                if (DoLinesIntersect(line1Start, line1End, line2Start, line2End))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private bool DoLinesIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        float d1 = CrossProduct(q2 - p2, p1 - p2);
        float d2 = CrossProduct(q2 - p2, q1 - p2);
        float d3 = CrossProduct(q1 - p1, p2 - p1);
        float d4 = CrossProduct(q1 - p1, q2 - p1);
        
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
        {
            return true;
        }
        
        if (Mathf.Approximately(d1, 0) && IsPointOnSegment(p2, p1, q2)) return true;
        if (Mathf.Approximately(d2, 0) && IsPointOnSegment(p2, q1, q2)) return true;
        if (Mathf.Approximately(d3, 0) && IsPointOnSegment(p1, p2, q1)) return true;
        if (Mathf.Approximately(d4, 0) && IsPointOnSegment(p1, q2, q1)) return true;
        
        return false;
    }
    
    private float CrossProduct(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }
    
    private bool IsPointOnSegment(Vector2 p, Vector2 point, Vector2 q)
    {
        return point.x <= Mathf.Max(p.x, q.x) && point.x >= Mathf.Min(p.x, q.x) &&
               point.y <= Mathf.Max(p.y, q.y) && point.y >= Mathf.Min(p.y, q.y);
    }
    
    // Public API
    public bool IsComplete() => isComplete;
    public List<Vector2> GetPoints() => new List<Vector2>(points);
    public float GetArea() => CalculatePolygonArea();
    
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
    
    // Context menu methods
    [ContextMenu("Start Outlining")]
    public void StartOutlining_Inspector() => StartOutlining();
    
    [ContextMenu("Reset")]
    public void Reset_Inspector() => Reset();
}
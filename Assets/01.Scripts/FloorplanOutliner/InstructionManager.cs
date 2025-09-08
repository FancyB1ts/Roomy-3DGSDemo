using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class InstructionManager : MonoBehaviour
{
    [Header("Text References")]
    public TextMeshProUGUI instructionText0;  // "Self-intersecting lines detected! Please avoid intersections for accurate calculation"
    public TextMeshProUGUI instructionText1;  // "Outline one room of which you know the dimensions. Click on the floor plan to begin drawing"
    public TextMeshProUGUI instructionText2;  // "Points placed: 1 • Click to continue outlining"
    public TextMeshProUGUI instructionText3;  // "Points placed: 2 • Click to continue outlining"
    public TextMeshProUGUI instructionText4;  // "Points placed: 3 • Click to continue outlining"
    public TextMeshProUGUI instructionText5;  // "Points placed: 4+ • Click the highlighted point to finish"
    
    [Header("Component References")]
    public PolygonDrawer polygonDrawer;
    
    [Header("Update Settings")]
    public float checkInterval = 0.1f;  // Check state 10 times per second
    
    // State tracking
    private int lastPointCount = -1;
    private bool lastDrawingState = false;
    private bool lastCompleteState = false;
    private bool lastIntersectionState = false;
    
    void Start()
    {
        // Auto-find PolygonDrawer if not assigned
        if (polygonDrawer == null)
        {
            polygonDrawer = FindObjectOfType<PolygonDrawer>();
            if (polygonDrawer != null)
                Debug.Log("[InstructionManager] Auto-found PolygonDrawer");
            else
                Debug.LogError("[InstructionManager] PolygonDrawer not found!");
        }
        
        // Start monitoring
        InvokeRepeating(nameof(CheckState), 0.1f, checkInterval);
        
        // Initialize with all messages hidden
        SetActiveMessage(-1);
    }
    
    void CheckState()
    {
        if (polygonDrawer == null) return;
        
        // Get current state
        bool isDrawing = IsDrawingActive();
        bool isComplete = polygonDrawer.IsComplete();
        int pointCount = polygonDrawer.GetPoints().Count;
        bool hasIntersections = HasSelfIntersections();
        
        // Check if state changed (optimization - only update UI when needed)
        bool stateChanged = (
            pointCount != lastPointCount ||
            isDrawing != lastDrawingState ||
            isComplete != lastCompleteState ||
            hasIntersections != lastIntersectionState
        );
        
        if (stateChanged)
        {
            UpdateInstructions(isDrawing, isComplete, pointCount, hasIntersections);
            
            // Update state tracking
            lastPointCount = pointCount;
            lastDrawingState = isDrawing;
            lastCompleteState = isComplete;
            lastIntersectionState = hasIntersections;
        }
    }
    
    private bool IsDrawingActive()
    {
        // Check if drawing mode is active by looking at line renderer
        return polygonDrawer.lineRenderer != null && polygonDrawer.lineRenderer.enabled;
    }
    
    private bool HasSelfIntersections()
    {
        List<Vector2> points = polygonDrawer.GetPoints();
        if (points.Count < 4) return false;
        
        // Use same intersection detection logic as PolygonDrawer
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
    
    private void UpdateInstructions(bool isDrawing, bool isComplete, int pointCount, bool hasIntersections)
    {
        // Priority 1: Polygon complete - hide all messages
        if (isComplete)
        {
            SetActiveMessage(-1);
            return;
        }
        
        // Priority 2: Intersection warning (only while drawing)
        if (hasIntersections && pointCount >= 3)
        {
            SetActiveMessage(0);
            return;
        }
        
        // Priority 3: Drawing instructions
        if (isDrawing)
        {
            if (pointCount == 0)
            {
                SetActiveMessage(1);  // "Outline one room..."
            }
            else if (pointCount == 1)
            {
                SetActiveMessage(2);  // "Points placed: 1..."
            }
            else if (pointCount == 2)
            {
                SetActiveMessage(3);  // "Points placed: 2..."
            }
            else if (pointCount == 3)
            {
                SetActiveMessage(4);  // "Points placed: 3..."
            }
            else // 4+ points
            {
                SetActiveMessage(5);  // "Points placed: 4+..."
            }
            return;
        }
        
        // Default: Hide all messages
        SetActiveMessage(-1);
    }
    
    private void SetActiveMessage(int messageIndex)
    {
        // Deactivate all messages first
        if (instructionText0 != null) instructionText0.gameObject.SetActive(false);
        if (instructionText1 != null) instructionText1.gameObject.SetActive(false);
        if (instructionText2 != null) instructionText2.gameObject.SetActive(false);
        if (instructionText3 != null) instructionText3.gameObject.SetActive(false);
        if (instructionText4 != null) instructionText4.gameObject.SetActive(false);
        if (instructionText5 != null) instructionText5.gameObject.SetActive(false);
        
        // Activate the specified message (0-5, or -1 for none)
        switch (messageIndex)
        {
            case 0:
                if (instructionText0 != null) instructionText0.gameObject.SetActive(true);
                break;
            case 1:
                if (instructionText1 != null) instructionText1.gameObject.SetActive(true);
                break;
            case 2:
                if (instructionText2 != null) instructionText2.gameObject.SetActive(true);
                break;
            case 3:
                if (instructionText3 != null) instructionText3.gameObject.SetActive(true);
                break;
            case 4:
                if (instructionText4 != null) instructionText4.gameObject.SetActive(true);
                break;
            case 5:
                if (instructionText5 != null) instructionText5.gameObject.SetActive(true);
                break;
            // case -1 or default: all messages remain deactivated
        }
    }
    
    void OnDestroy()
    {
        // Clean up - hide all messages when destroyed
        SetActiveMessage(-1);
    }
    
    // Debug methods
    [ContextMenu("Test Message 0")]
    public void TestMessage0() => SetActiveMessage(0);
    
    [ContextMenu("Test Message 1")]
    public void TestMessage1() => SetActiveMessage(1);
    
    [ContextMenu("Test Message 2")]
    public void TestMessage2() => SetActiveMessage(2);
    
    [ContextMenu("Test Message 3")]
    public void TestMessage3() => SetActiveMessage(3);
    
    [ContextMenu("Test Message 4")]
    public void TestMessage4() => SetActiveMessage(4);
    
    [ContextMenu("Test Message 5")]
    public void TestMessage5() => SetActiveMessage(5);
    
    [ContextMenu("Hide All Messages")]
    public void HideAllMessages() => SetActiveMessage(-1);
    
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        if (polygonDrawer == null)
        {
            Debug.Log("[InstructionManager] PolygonDrawer is null");
            return;
        }
        
        bool isDrawing = IsDrawingActive();
        bool isComplete = polygonDrawer.IsComplete();
        int pointCount = polygonDrawer.GetPoints().Count;
        bool hasIntersections = HasSelfIntersections();
        
        Debug.Log($"[InstructionManager] State: Drawing={isDrawing}, Complete={isComplete}, Points={pointCount}, Intersections={hasIntersections}");
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class LabelMeShape
{
    public string label;
    public float[][] points; // Array of [x,y] coordinates
    public string shape_type;
    public string group_id;
    public string description;
    public object flags;
    public object mask;
}

[Serializable]
public class LabelMeData
{
    public string version;
    public object flags;
    public LabelMeShape[] shapes;
    public string imagePath;
    public string imageData;
    public int imageHeight;
    public int imageWidth;
}

[Serializable]
public class FloorplanRectangle
{
    public float x;
    public float y;
    public float width;
    public float height;
    public Vector2[] corners;

    public Vector2 Min => new Vector2(x, y);
    public Vector2 Max => new Vector2(x + width, y + height);

    public List<FloorplanEdge> GetEdges()
    {
        if (corners != null && corners.Length == 4)
        {
            return new List<FloorplanEdge>
            {
                new FloorplanEdge(corners[0], corners[1]), // Bottom edge
                new FloorplanEdge(corners[1], corners[2]), // Right edge
                new FloorplanEdge(corners[2], corners[3]), // Top edge
                new FloorplanEdge(corners[3], corners[0])  // Left edge
            };
        }
        else
        {
            Debug.LogError("Rectangle corners not properly initialized!");
            return new List<FloorplanEdge>();
        }
    }

    public bool Overlaps(FloorplanRectangle other)
    {
        return !(Max.x <= other.Min.x || Min.x >= other.Max.x ||
                 Max.y <= other.Min.y || Min.y >= other.Max.y);
    }

    public bool ContainsPoint(Vector2 point, float tolerance = 0.01f)
    {
        return point.x >= (Min.x - tolerance) && point.x <= (Max.x + tolerance) &&
               point.y >= (Min.y - tolerance) && point.y <= (Max.y + tolerance);
    }
}

[Serializable]
public class FloorplanEdge
{
    public Vector2 start;
    public Vector2 end;

    public FloorplanEdge(Vector2 s, Vector2 e)
    {
        start = s;
        end = e;
    }

    public bool IsHorizontal => Mathf.Approximately(start.y, end.y);
    public bool IsVertical => Mathf.Approximately(start.x, end.x);
    public float Length => Vector2.Distance(start, end);

    public bool ContainsPoint(Vector2 point, float tolerance = 0.1f)
    {
        // Check if point lies on this edge
        float distToLine = DistancePointToLine(point, start, end);
        if (distToLine > tolerance) return false;

        // Check if point is between start and end
        float minX = Mathf.Min(start.x, end.x) - tolerance;
        float maxX = Mathf.Max(start.x, end.x) + tolerance;
        float minY = Mathf.Min(start.y, end.y) - tolerance;
        float maxY = Mathf.Max(start.y, end.y) + tolerance;

        return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
    }

    private float DistancePointToLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        if (line.magnitude < 0.001f) return Vector2.Distance(point, lineStart);

        float t = Vector2.Dot(point - lineStart, line) / Vector2.Dot(line, line);
        Vector2 projection = lineStart + t * line;
        return Vector2.Distance(point, projection);
    }
}

public struct IntersectionPoint
{
    public Vector2 point;
    public int edge1Index;
    public int edge2Index;

    public IntersectionPoint(Vector2 p, int e1, int e2)
    {
        point = p;
        edge1Index = e1;
        edge2Index = e2;
    }
}

public class RectangleEdgeProcessor : MonoBehaviour
{
    [Header("File Settings")]
    public TextAsset jsonFile;

    [Header("Processing Settings")]
    public float intersectionTolerance = 0.1f;
    public string targetLabel = "wall";

    [Header("Debug")]
    public bool debugMode = true;
    public bool showOriginalRectangles = true;
    public bool showProcessedEdges = true;
    public bool showIntersectionPoints = true;
    public bool showNormalDirections = true;
    public Color originalRectangleColor = Color.blue;
    public Color edgeColor = Color.red;
    public Color intersectionColor = Color.yellow;
    public Color inwardNormalColor = Color.green;
    public Color outwardNormalColor = Color.magenta;

    private List<FloorplanEdge> processedEdges = new List<FloorplanEdge>();
    private List<Vector2> intersectionPoints = new List<Vector2>();
    private List<FloorplanRectangle> originalRectangles = new List<FloorplanRectangle>();
    private List<EdgeNormalInfo> edgeNormalData = new List<EdgeNormalInfo>(); // Store for debug visualization

    void Start()
    {
        Invoke(nameof(ProcessJsonFile), 0.1f);
    }

    public void ProcessJsonFile()
    {
        try
        {
            if (jsonFile == null)
            {
                Debug.LogWarning("No JSON file assigned. Testing with sample data instead...");
                TestWithSampleData();
                return;
            }

            string jsonContent = jsonFile.text;
            LabelMeData data = JsonConvert.DeserializeObject<LabelMeData>(jsonContent);

            if (data?.shapes == null)
            {
                Debug.LogError("Invalid JSON format or no shapes found");
                return;
            }

            var rectangles = ConvertLabelMeShapesToRectangles(data.shapes);
            ProcessRectangles(rectangles);

            Debug.Log($"Processed {rectangles.Count} rectangles into {processedEdges.Count} merged edges");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing JSON file: {e.Message}");
        }
    }

    private void TestWithSampleData()
    {
        var testRectangles = new List<FloorplanRectangle>();

        // Create overlapping rectangles for testing
        var rect1 = new FloorplanRectangle { x = 0, y = 0, width = 10, height = 5 };
        rect1.corners = new Vector2[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 5), new Vector2(0, 5) };

        var rect2 = new FloorplanRectangle { x = 5, y = 0, width = 10, height = 5 };
        rect2.corners = new Vector2[] { new Vector2(5, 0), new Vector2(15, 0), new Vector2(15, 5), new Vector2(5, 5) };

        testRectangles.Add(rect1);
        testRectangles.Add(rect2);

        ProcessRectangles(testRectangles);
        Debug.Log($"Test: Processed {testRectangles.Count} sample rectangles into {processedEdges.Count} merged edges");
    }

    private List<FloorplanRectangle> ConvertLabelMeShapesToRectangles(LabelMeShape[] shapes)
    {
        var rectangles = new List<FloorplanRectangle>();

        foreach (var shape in shapes)
        {
            if (shape.shape_type == "rectangle" && shape.points != null && shape.points.Length >= 2)
            {
                // Filter by target label
                if (!string.IsNullOrEmpty(targetLabel) && shape.label != targetLabel)
                {
                    Debug.Log($"Skipping shape '{shape.label}' (not matching target label '{targetLabel}')");
                    continue;
                }

                var rect = ConvertTwoPointToFourPoint(shape);
                if (rect != null)
                {
                    rectangles.Add(rect);
                    Debug.Log($"Converted shape '{shape.label}': " +
                             $"P1=({rect.corners[0].x:F1},{rect.corners[0].y:F1}) " +
                             $"P2=({rect.corners[1].x:F1},{rect.corners[1].y:F1}) " +
                             $"P3=({rect.corners[2].x:F1},{rect.corners[2].y:F1}) " +
                             $"P4=({rect.corners[3].x:F1},{rect.corners[3].y:F1})");
                }
            }
        }

        Debug.Log($"Converted {rectangles.Count} '{targetLabel}' rectangles from {shapes.Length} total shapes");
        return rectangles;
    }

    private FloorplanRectangle ConvertTwoPointToFourPoint(LabelMeShape shape)
    {
        float x1 = shape.points[0][0];
        float y1 = shape.points[0][1];
        float x2 = shape.points[1][0];
        float y2 = shape.points[1][1];

        float minX = Mathf.Min(x1, x2);
        float minY = Mathf.Min(y1, y2);
        float maxX = Mathf.Max(x1, x2);
        float maxY = Mathf.Max(y1, y2);

        var corners = new Vector2[4]
        {
            new Vector2(minX, minY), // Bottom-left
            new Vector2(maxX, minY), // Bottom-right
            new Vector2(maxX, maxY), // Top-right
            new Vector2(minX, maxY)  // Top-left
        };

        return new FloorplanRectangle
        {
            x = minX,
            y = minY,
            width = maxX - minX,
            height = maxY - minY,
            corners = corners
        };
    }

    private void ProcessRectangles(List<FloorplanRectangle> rectangles)
    {
        // Store original rectangles for debug visualization
        originalRectangles.Clear();
        originalRectangles.AddRange(rectangles);

        var groups = GroupOverlappingRectangles(rectangles);
        processedEdges.Clear();
        intersectionPoints.Clear();

        foreach (var group in groups)
        {
            var mergedEdges = PerformBooleanUnion(group);
            processedEdges.AddRange(mergedEdges);
        }

        Debug.Log($"Total processed edges: {processedEdges.Count}, intersections: {intersectionPoints.Count}");
    }

    private List<List<FloorplanRectangle>> GroupOverlappingRectangles(List<FloorplanRectangle> rectangles)
    {
        var groups = new List<List<FloorplanRectangle>>();
        var processed = new HashSet<FloorplanRectangle>();

        foreach (var rect in rectangles)
        {
            if (processed.Contains(rect)) continue;

            var group = new List<FloorplanRectangle>();
            var queue = new Queue<FloorplanRectangle>();
            queue.Enqueue(rect);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (processed.Contains(current)) continue;

                processed.Add(current);
                group.Add(current);

                foreach (var other in rectangles)
                {
                    if (!processed.Contains(other) && current.Overlaps(other))
                    {
                        queue.Enqueue(other);
                    }
                }
            }

            groups.Add(group);
        }

        Debug.Log($"Found {groups.Count} groups of overlapping rectangles");
        return groups;
    }

    private List<FloorplanEdge> PerformBooleanUnion(List<FloorplanRectangle> rectangles)
    {
        if (rectangles.Count == 1)
        {
            return rectangles[0].GetEdges();
        }

        Debug.Log($"Performing selective merging on {rectangles.Count} rectangles");

        // Step 1: Collect all edges
        var allEdges = new List<FloorplanEdge>();
        foreach (var rect in rectangles)
        {
            allEdges.AddRange(rect.GetEdges());
        }

        Debug.Log($"Collected {allEdges.Count} total edges");

        // Step 2: Find all intersection points
        var intersections = FindAllIntersections(allEdges);
        Debug.Log($"Found {intersections.Count} intersection points");

        // Step 3: Split edges at intersection points
        var splitEdges = SplitEdgesAtIntersections(allEdges, intersections);
        Debug.Log($"Split into {splitEdges.Count} edge segments");

        // Step 4: NEW APPROACH - Detect rectangles and classify edges by normal directions
        var classifiedEdges = ClassifyEdgesByNormals(splitEdges, rectangles);

        // Step 5: Keep only outer-facing edges
        var outerEdges = new List<FloorplanEdge>();
        var discardedEdges = new List<FloorplanEdge>();

        foreach (var classified in classifiedEdges)
        {
            if (classified.classification == "OUTER" || classified.classification == "BOUNDARY")
            {
                outerEdges.Add(classified.edge);
            }
            else // INNER
            {
                discardedEdges.Add(classified.edge);
            }
        }

        Debug.Log($"FILTERING RESULTS:");
        Debug.Log($"  - Kept {outerEdges.Count} outer/boundary edges");
        Debug.Log($"  - DISCARDED {discardedEdges.Count} inner-facing edges");

        // Step 6: Merge only outer boundary segments
        var mergedEdges = MergeCollinearEdges(outerEdges);
        Debug.Log($"Merged into {mergedEdges.Count} final edges");

        return mergedEdges;
    }

    private struct EdgeNormalInfo
    {
        public FloorplanEdge edge;
        public Vector2 inwardNormal;  // Points into the rectangle
        public Vector2 outwardNormal; // Points out of the rectangle
        public bool isPartOfRectangle;

        public EdgeNormalInfo(FloorplanEdge e, Vector2 inward, Vector2 outward, bool partOfRect)
        {
            edge = e;
            inwardNormal = inward;
            outwardNormal = outward;
            isPartOfRectangle = partOfRect;
        }
    }

    private List<ClassifiedEdge> ClassifyEdgesByNormals(List<FloorplanEdge> splitEdges,
                                                   List<FloorplanRectangle> rectangles)
    {
        var results = new List<ClassifiedEdge>();
        edgeNormalData.Clear();

        for (int i = 0; i < splitEdges.Count; i++)
        {
            var edge = splitEdges[i];
            var normalInfo = DetermineEdgeNormals(edge, rectangles);
            edgeNormalData.Add(normalInfo);

            // midpoint for testing
            Vector2 mid = (edge.start + edge.end) * 0.5f;
            float testDist = intersectionTolerance * 2f;

            // sample one pixel inward/outward
            Vector2 inPt = mid + normalInfo.inwardNormal * testDist;
            Vector2 outPt = mid + normalInfo.outwardNormal * testDist;

            int inCount = CountContainingRectangles(inPt, rectangles);
            int outCount = CountContainingRectangles(outPt, rectangles);

            string cls;

            // If neither side is inside any rect, it truly lies OUTSIDE
            if (inCount == 0 && outCount == 0)
            {
                cls = "OUTER";
            }
            // If outward side is “more inside” than inward → this edge faces INTO an overlap (so it’s interior)
            else if (outCount > inCount)
            {
                cls = "INNER";
            }
            // If inward side is “more inside” than outward → true boundary
            else if (inCount > outCount)
            {
                cls = "OUTER";
            }
            else
            {
                // inCount == outCount != 0 → it’s shared by two rectangles side‐by‐side → boundary
                cls = "BOUNDARY";
            }

            results.Add(new ClassifiedEdge(edge, cls, inCount, inCount, outCount));

            if (i < 20)
                Debug.Log($"Edge {i}: ({edge.start})→({edge.end}) | in={inCount}, out={outCount} | CLASS={cls}");
        }

        int innerC = results.Count(e => e.classification == "INNER");
        int boundaryC = results.Count(e => e.classification == "BOUNDARY");
        int outerC = results.Count(e => e.classification == "OUTER");
        Debug.Log($"Normal‐based: {outerC} OUTER, {boundaryC} BOUNDARY, {innerC} INNER");
        return results;
    }

    private EdgeNormalInfo DetermineEdgeNormals(FloorplanEdge edge, List<FloorplanRectangle> rectangles)
    {
        // Find which rectangle(s) this edge belongs to
        FloorplanRectangle parentRect = null;
        foreach (var rect in rectangles)
        {
            if (IsEdgePartOfRectangle(edge, rect))
            {
                parentRect = rect;
                break;
            }
        }

        if (parentRect == null)
        {
            // Edge doesn't belong to any complete rectangle
            Vector2 edgeVector = edge.end - edge.start;
            Vector2 arbitraryNormal = new Vector2(-edgeVector.y, edgeVector.x).normalized;
            return new EdgeNormalInfo(edge, arbitraryNormal, -arbitraryNormal, false);
        }

        // Calculate normals based on rectangle orientation
        Vector2 edgeDirection = (edge.end - edge.start).normalized;
        Vector2 normal = new Vector2(-edgeDirection.y, edgeDirection.x).normalized;

        // Determine which direction points INTO the rectangle
        Vector2 edgeMidpoint = (edge.start + edge.end) * 0.5f;
        Vector2 rectCenter = new Vector2(parentRect.x + parentRect.width * 0.5f, parentRect.y + parentRect.height * 0.5f);
        Vector2 toCenter = (rectCenter - edgeMidpoint).normalized;

        // The inward normal should point toward the rectangle center
        Vector2 inwardNormal, outwardNormal;
        if (Vector2.Dot(normal, toCenter) > 0)
        {
            inwardNormal = normal;
            outwardNormal = -normal;
        }
        else
        {
            inwardNormal = -normal;
            outwardNormal = normal;
        }

        return new EdgeNormalInfo(edge, inwardNormal, outwardNormal, true);
    }

    private bool IsEdgePartOfRectangle(FloorplanEdge edge, FloorplanRectangle rect)
    {
        float tolerance = intersectionTolerance;

        // Check if edge lies on any of the rectangle's four sides
        return IsEdgeOnRectangleSide(edge, rect, tolerance);
    }

    private int CountContainingRectangles(Vector2 point, List<FloorplanRectangle> rectangles)
    {
        int count = 0;
        foreach (var rect in rectangles)
        {
            if (rect.ContainsPoint(point, intersectionTolerance))
            {
                count++;
            }
        }
        return count;
    }

    private struct ClassifiedEdge
    {
        public FloorplanEdge edge;
        public string classification;
        public int containingRectCount;
        public int leftSideCount;
        public int rightSideCount;

        public ClassifiedEdge(FloorplanEdge e, string c, int containingCount, int leftCount, int rightCount)
        {
            edge = e;
            classification = c;
            containingRectCount = containingCount;
            leftSideCount = leftCount;
            rightSideCount = rightCount;
        }
    }

    private List<ClassifiedEdge> ClassifySplitEdges(List<FloorplanEdge> splitEdges, List<FloorplanRectangle> rectangles)
    {
        Debug.Log("=== CLASSIFYING SPLIT EDGES ===");
        var classifiedEdges = new List<ClassifiedEdge>();

        for (int i = 0; i < splitEdges.Count; i++)
        {
            var edge = splitEdges[i];
            Vector2 midpoint = (edge.start + edge.end) * 0.5f;

            // Count how many rectangles contain this edge's midpoint
            int containingRectCount = 0;
            foreach (var rect in rectangles)
            {
                if (rect.ContainsPoint(midpoint, intersectionTolerance))
                {
                    containingRectCount++;
                }
            }

            // SUPER SIMPLE CLASSIFICATION - just use left/right side test
            Vector2 edgeVector = edge.end - edge.start;
            Vector2 normal = new Vector2(-edgeVector.y, edgeVector.x).normalized;
            float testDistance = 1.0f; // Use larger test distance
            Vector2 leftSide = midpoint + normal * testDistance;
            Vector2 rightSide = midpoint - normal * testDistance;

            int leftCount = 0, rightCount = 0;
            foreach (var rect in rectangles)
            {
                if (rect.ContainsPoint(leftSide, intersectionTolerance)) leftCount++;
                if (rect.ContainsPoint(rightSide, intersectionTolerance)) rightCount++;
            }

            // SIMPLE CLASSIFICATION
            string classification;
            if (containingRectCount == 0)
            {
                classification = "OUTSIDE";
            }
            else if (leftCount != rightCount)
            {
                classification = "BOUNDARY"; // Different counts on each side
            }
            else
            {
                classification = "INSIDE"; // Same count on both sides = internal
            }

            var classifiedEdge = new ClassifiedEdge(edge, classification, containingRectCount, leftCount, rightCount);
            classifiedEdges.Add(classifiedEdge);

            // Debug output for first 30 edges
            if (i < 30)
            {
                Debug.Log($"Edge {i}: ({edge.start.x:F1},{edge.start.y:F1}) -> ({edge.end.x:F1},{edge.end.y:F1}) " +
                         $"| In {containingRectCount} rects | Left={leftCount}, Right={rightCount} " +
                         $"| CLASS: {classification}");
            }
        }

        // Summary
        int outsideCount = classifiedEdges.Count(e => e.classification == "OUTSIDE");
        int boundaryCount = classifiedEdges.Count(e => e.classification == "BOUNDARY");
        int insideCount = classifiedEdges.Count(e => e.classification == "INSIDE");

        Debug.Log($"Classification summary: {outsideCount} OUTSIDE, {boundaryCount} BOUNDARY, {insideCount} INSIDE");

        if (insideCount == 0)
        {
            Debug.LogWarning("WARNING: No edges classified as INSIDE - all edges have different left/right counts!");
        }

        return classifiedEdges;
    }

    private bool IsEdgeOnAnyRectangleBoundary(FloorplanEdge edge, List<FloorplanRectangle> rectangles)
    {
        float tolerance = intersectionTolerance;

        foreach (var rect in rectangles)
        {
            // Check if edge lies on any of the rectangle's four sides
            if (IsEdgeOnRectangleSide(edge, rect, tolerance))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsEdgeOnRectangleSide(FloorplanEdge edge, FloorplanRectangle rect, float tolerance)
    {
        // Check if edge lies on left side
        if (Mathf.Abs(edge.start.x - rect.Min.x) < tolerance &&
            Mathf.Abs(edge.end.x - rect.Min.x) < tolerance &&
            IsSegmentWithinRange(edge.start.y, edge.end.y, rect.Min.y, rect.Max.y, tolerance))
            return true;

        // Check if edge lies on right side
        if (Mathf.Abs(edge.start.x - rect.Max.x) < tolerance &&
            Mathf.Abs(edge.end.x - rect.Max.x) < tolerance &&
            IsSegmentWithinRange(edge.start.y, edge.end.y, rect.Min.y, rect.Max.y, tolerance))
            return true;

        // Check if edge lies on bottom side
        if (Mathf.Abs(edge.start.y - rect.Min.y) < tolerance &&
            Mathf.Abs(edge.end.y - rect.Min.y) < tolerance &&
            IsSegmentWithinRange(edge.start.x, edge.end.x, rect.Min.x, rect.Max.x, tolerance))
            return true;

        // Check if edge lies on top side
        if (Mathf.Abs(edge.start.y - rect.Max.y) < tolerance &&
            Mathf.Abs(edge.end.y - rect.Max.y) < tolerance &&
            IsSegmentWithinRange(edge.start.x, edge.end.x, rect.Min.x, rect.Max.x, tolerance))
            return true;

        return false;
    }

    private bool IsSegmentWithinRange(float start, float end, float rangeMin, float rangeMax, float tolerance)
    {
        float segMin = Mathf.Min(start, end);
        float segMax = Mathf.Max(start, end);

        // Check if the segment overlaps with the range
        return segMax >= (rangeMin - tolerance) && segMin <= (rangeMax + tolerance);
    }

    private bool DoesCrossBoundary(FloorplanEdge edge, List<FloorplanRectangle> rectangles)
    {
        Vector2 midpoint = (edge.start + edge.end) * 0.5f;
        Vector2 edgeVector = edge.end - edge.start;
        Vector2 normal = new Vector2(-edgeVector.y, edgeVector.x).normalized;

        float testDistance = intersectionTolerance * 5; // Larger test distance
        Vector2 testPoint1 = midpoint + normal * testDistance;
        Vector2 testPoint2 = midpoint - normal * testDistance;

        // Count rectangles containing each test point
        int count1 = 0, count2 = 0;
        foreach (var rect in rectangles)
        {
            if (rect.ContainsPoint(testPoint1, intersectionTolerance)) count1++;
            if (rect.ContainsPoint(testPoint2, intersectionTolerance)) count2++;
        }

        // Cross boundary if different counts on each side
        return count1 != count2;
    }

    private List<IntersectionPoint> FindAllIntersections(List<FloorplanEdge> edges)
    {
        var intersections = new List<IntersectionPoint>();

        for (int i = 0; i < edges.Count; i++)
        {
            for (int j = i + 1; j < edges.Count; j++)
            {
                var intersection = FindLineIntersection(edges[i], edges[j]);
                if (intersection.HasValue)
                {
                    intersections.Add(new IntersectionPoint(intersection.Value, i, j));
                    intersectionPoints.Add(intersection.Value);
                }
            }
        }

        return intersections;
    }

    private Vector2? FindLineIntersection(FloorplanEdge edge1, FloorplanEdge edge2)
    {
        Vector2 p1 = edge1.start, p2 = edge1.end;
        Vector2 p3 = edge2.start, p4 = edge2.end;

        float denom = (p1.x - p2.x) * (p3.y - p4.y) - (p1.y - p2.y) * (p3.x - p4.x);
        if (Mathf.Abs(denom) < 0.0001f) return null; // Parallel lines

        float t = ((p1.x - p3.x) * (p3.y - p4.y) - (p1.y - p3.y) * (p3.x - p4.x)) / denom;
        float u = -((p1.x - p2.x) * (p1.y - p3.y) - (p1.y - p2.y) * (p1.x - p3.x)) / denom;

        // Only count as intersection if lines cross in the MIDDLE (not at endpoints)
        float tolerance = 0.01f;
        if (t > tolerance && t < (1 - tolerance) && u > tolerance && u < (1 - tolerance))
        {
            Vector2 intersection = new Vector2(p1.x + t * (p2.x - p1.x), p1.y + t * (p2.y - p1.y));

            Debug.Log($"TRUE INTERSECTION found at ({intersection.x:F1},{intersection.y:F1}) " +
                     $"between edges ({p1.x:F1},{p1.y:F1})->({p2.x:F1},{p2.y:F1}) " +
                     $"and ({p3.x:F1},{p3.y:F1})->({p4.x:F1},{p4.y:F1}) | t={t:F3}, u={u:F3}");

            return intersection;
        }

        return null;
    }

    private List<FloorplanEdge> SplitEdgesAtIntersections(List<FloorplanEdge> edges, List<IntersectionPoint> intersections)
    {
        var splitEdges = new List<FloorplanEdge>();

        for (int i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            var intersectionsOnThisEdge = intersections
                .Where(inter => inter.edge1Index == i || inter.edge2Index == i)
                .Select(inter => inter.point)
                .Where(point => edge.ContainsPoint(point))
                .OrderBy(point => Vector2.Distance(edge.start, point))
                .ToList();

            if (intersectionsOnThisEdge.Count == 0)
            {
                splitEdges.Add(edge);
            }
            else
            {
                Vector2 currentStart = edge.start;
                foreach (var intersection in intersectionsOnThisEdge)
                {
                    if (Vector2.Distance(currentStart, intersection) > 0.01f)
                    {
                        splitEdges.Add(new FloorplanEdge(currentStart, intersection));
                    }
                    currentStart = intersection;
                }

                if (Vector2.Distance(currentStart, edge.end) > 0.01f)
                {
                    splitEdges.Add(new FloorplanEdge(currentStart, edge.end));
                }
            }
        }

        return splitEdges;
    }

    private List<FloorplanEdge> MergeCollinearEdges(List<FloorplanEdge> edges)
    {
        var mergedEdges = new List<FloorplanEdge>();
        var used = new HashSet<int>();

        for (int i = 0; i < edges.Count; i++)
        {
            if (used.Contains(i)) continue;

            var currentEdge = edges[i];
            used.Add(i);

            // Try to extend this edge by merging with collinear edges
            bool merged = true;
            while (merged)
            {
                merged = false;
                for (int j = 0; j < edges.Count; j++)
                {
                    if (used.Contains(j)) continue;

                    var extendedEdge = TryMergeEdges(currentEdge, edges[j]);
                    if (extendedEdge != null)
                    {
                        currentEdge = extendedEdge;
                        used.Add(j);
                        merged = true;
                        break;
                    }
                }
            }

            mergedEdges.Add(currentEdge);
        }

        return mergedEdges;
    }

    private FloorplanEdge TryMergeEdges(FloorplanEdge edge1, FloorplanEdge edge2)
    {
        float tolerance = 0.1f;

        // Check if edges are collinear and connected
        if (Vector2.Distance(edge1.end, edge2.start) < tolerance)
        {
            if (AreCollinear(edge1.start, edge1.end, edge2.end, tolerance))
            {
                return new FloorplanEdge(edge1.start, edge2.end);
            }
        }
        else if (Vector2.Distance(edge1.start, edge2.end) < tolerance)
        {
            if (AreCollinear(edge2.start, edge2.end, edge1.end, tolerance))
            {
                return new FloorplanEdge(edge2.start, edge1.end);
            }
        }

        return null;
    }

    private bool AreCollinear(Vector2 p1, Vector2 p2, Vector2 p3, float tolerance)
    {
        Vector2 v1 = (p2 - p1).normalized;
        Vector2 v2 = (p3 - p2).normalized;
        return Vector2.Dot(v1, v2) > (1 - tolerance);
    }

    public List<FloorplanEdge> GetProcessedEdges()
    {
        return new List<FloorplanEdge>(processedEdges);
    }

    void OnDrawGizmos()
    {
        if (!debugMode) return;

        // Draw original rectangles
        if (showOriginalRectangles)
        {
            Gizmos.color = originalRectangleColor;
            foreach (var rect in originalRectangles)
            {
                if (rect.corners != null && rect.corners.Length == 4)
                {
                    // Draw rectangle outline
                    for (int i = 0; i < 4; i++)
                    {
                        int nextIndex = (i + 1) % 4;
                        Vector3 start = new Vector3(rect.corners[i].x, rect.corners[i].y, 0);
                        Vector3 end = new Vector3(rect.corners[nextIndex].x, rect.corners[nextIndex].y, 0);
                        Gizmos.DrawLine(start, end);
                    }

                    // Draw corner points
                    foreach (var corner in rect.corners)
                    {
                        Gizmos.DrawSphere(new Vector3(corner.x, corner.y, 0), 0.5f);
                    }
                }
            }
        }

        // Draw processed edges (result of boolean union)
        if (showProcessedEdges)
        {
            Gizmos.color = edgeColor;
            foreach (var edge in processedEdges)
            {
                if (edge != null)
                {
                    Gizmos.DrawLine(new Vector3(edge.start.x, edge.start.y, 0),
                                   new Vector3(edge.end.x, edge.end.y, 0));
                }
            }
        }

        // Draw intersection points
        if (showIntersectionPoints)
        {
            Gizmos.color = intersectionColor;
            foreach (var point in intersectionPoints)
            {
                Gizmos.DrawSphere(new Vector3(point.x, point.y, 0), 1f);
            }
        }

        // Draw normal directions
        if (showNormalDirections && edgeNormalData.Count > 0)
        {
            float normalLength = 3.0f; // Length of normal arrows

            // Limit to first 50 edges to avoid visual clutter
            int maxEdgesToShow = Mathf.Min(50, edgeNormalData.Count);

            for (int i = 0; i < maxEdgesToShow; i++)
            {
                var normalInfo = edgeNormalData[i];
                Vector2 midpoint = (normalInfo.edge.start + normalInfo.edge.end) * 0.5f;
                Vector3 midpoint3D = new Vector3(midpoint.x, midpoint.y, 0);

                // Draw inward normal (GREEN arrow)
                Gizmos.color = inwardNormalColor;
                Vector3 inwardEnd = midpoint3D + new Vector3(normalInfo.inwardNormal.x, normalInfo.inwardNormal.y, 0) * normalLength;
                Gizmos.DrawLine(midpoint3D, inwardEnd);
                Gizmos.DrawSphere(inwardEnd, 0.3f); // Arrow head

                // Draw outward normal (MAGENTA arrow)
                Gizmos.color = outwardNormalColor;
                Vector3 outwardEnd = midpoint3D + new Vector3(normalInfo.outwardNormal.x, normalInfo.outwardNormal.y, 0) * normalLength;
                Gizmos.DrawLine(midpoint3D, outwardEnd);
                Gizmos.DrawSphere(outwardEnd, 0.3f); // Arrow head

                // Draw a small white dot at the edge midpoint for reference
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(midpoint3D, 0.2f);
            }

            if (edgeNormalData.Count > maxEdgesToShow)
            {
                Debug.Log($"Showing normals for first {maxEdgesToShow} edges (total: {edgeNormalData.Count})");
            }
        }
    }
}
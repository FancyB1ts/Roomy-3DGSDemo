using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;  // (make sure you installed the package)

/// <summary>
/// Parses a floorplan JSON, extracts every “wall” rectangle as 4 axis-aligned edges,
/// splits each edge at every intersection, and then draws two sets of lines as gizmos:
///   • Originals in cyan  
///   • Split edges in yellow  
/// 
/// This runs entirely in Unity—no external console required. You can toggle which
/// gizmos to see. If you need them in the Game view at runtime, you can also
/// add a simple LineRenderer fallback (see notes at the bottom).
/// </summary>
public class FloorplanLineDebugger : MonoBehaviour
{
    [Header("Assign your floorplan JSON TextAsset here")]
    public TextAsset floorplanJson;

    // Raw edges (each rectangle → 4 edges)
    private List<Edge> _originalEdges = new List<Edge>();

    // Edges after splitting at intersections
    private List<Edge> _splitEdges = new List<Edge>();

    [Header("Visualization (Scene-view Gizmos)")]
    public bool drawOriginals = true;
    public bool drawSplits = true;

    // Colors for gizmos
    private readonly Color _origColor = Color.cyan;
    private readonly Color _splitColor = Color.yellow;

    void Start()
    {
        if (floorplanJson == null)
        {
            Debug.LogError("FloorplanLineDebugger: Drag your JSON TextAsset into the Inspector.");
            return;
        }

        ParseAndSplit();
    }

    /// <summary>
    /// 1) Deserialize JSON  
    /// 2) Build every “wall” → 4 axis-aligned edges  
    /// 3) Split those edges at all intersections  
    /// 4) (Optionally) Log counts and edges  
    /// </summary>
    private void ParseAndSplit()
    {
        // 1) DESERIALIZE
        FloorplanData data;
        try
        {
            data = JsonConvert.DeserializeObject<FloorplanData>(floorplanJson.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FloorplanLineDebugger] JSON parse failed: {e.Message}");
            return;
        }

        if (data?.shapes == null)
        {
            Debug.LogError("[FloorplanLineDebugger] JSON did not contain a ‘shapes’ array.");
            return;
        }

        // 2) BUILD ORIGINAL EDGES
        _originalEdges.Clear();
        foreach (var shape in data.shapes)
        {
            if (shape.label == null || shape.label.ToLower() != "wall")
                continue;

            if (shape.points == null || shape.points.Count != 2)
            {
                Debug.LogWarning($"[LineDebugger] Skipping a ‘wall’ with {shape.points?.Count ?? 0} points.");
                continue;
            }

            Vector2 pA = new Vector2(shape.points[0][0], shape.points[0][1]);
            Vector2 pB = new Vector2(shape.points[1][0], shape.points[1][1]);

            float minX = Mathf.Min(pA.x, pB.x);
            float maxX = Mathf.Max(pA.x, pB.x);
            float minY = Mathf.Min(pA.y, pB.y);
            float maxY = Mathf.Max(pA.y, pB.y);

            Vector2 bl = new Vector2(minX, minY);
            Vector2 br = new Vector2(maxX, minY);
            Vector2 tr = new Vector2(maxX, maxY);
            Vector2 tl = new Vector2(minX, maxY);

            // Clockwise corners → four edges
            _originalEdges.Add(new Edge(bl, br)); // bottom
            _originalEdges.Add(new Edge(br, tr)); // right
            _originalEdges.Add(new Edge(tr, tl)); // top
            _originalEdges.Add(new Edge(tl, bl)); // left
        }

        Debug.Log($"[LineDebugger] Parsed {_originalEdges.Count} rectangle-edges.");

        // 3) SPLIT
        _splitEdges = SplitEdgesAtIntersections(_originalEdges);

        Debug.Log($"[LineDebugger] After splitting, total edges = {_splitEdges.Count}.");
    }

    /// <summary>
    /// Draw gizmos for original and split edges in the Scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (_originalEdges != null && drawOriginals)
        {
            Gizmos.color = _origColor;
            foreach (var e in _originalEdges)
            {
                Gizmos.DrawLine(new Vector3(e.p1.x, 0, e.p1.y),
                                new Vector3(e.p2.x, 0, e.p2.y));
            }
        }

        if (_splitEdges != null && drawSplits)
        {
            Gizmos.color = _splitColor;
            foreach (var e in _splitEdges)
            {
                Gizmos.DrawLine(new Vector3(e.p1.x, 0, e.p1.y),
                                new Vector3(e.p2.x, 0, e.p2.y));
            }
        }
    }

    // === JSON DATA CLASSES ===
    [System.Serializable]
    private class FloorplanData
    {
        public List<Shape> shapes;
    }

    [System.Serializable]
    private class Shape
    {
        public string label;
        public List<List<float>> points;
    }

    // === SIMPLE EDGE STRUCT ===
    private struct Edge
    {
        public Vector2 p1;
        public Vector2 p2;
        public Edge(Vector2 a, Vector2 b)
        {
            p1 = a;
            p2 = b;
        }
    }

    // === SPLIT LOGIC ===
    private static List<Edge> SplitEdgesAtIntersections(List<Edge> inputEdges)
    {
        var horizontals = new List<Edge>();
        var verticals = new List<Edge>();

        foreach (var e in inputEdges)
        {
            if (Mathf.Abs(e.p1.y - e.p2.y) < 1e-5f)
                horizontals.Add(e);
            else if (Mathf.Abs(e.p1.x - e.p2.x) < 1e-5f)
                verticals.Add(e);
            else
                Debug.LogWarning($"[Split] Skipped non-axis-aligned: ({e.p1})→({e.p2})");
        }

        var output = new List<Edge>();

        // Split horizontals by all verticals
        foreach (var h in horizontals)
        {
            float y = h.p1.y;
            float leftX = Mathf.Min(h.p1.x, h.p2.x);
            float rightX = Mathf.Max(h.p1.x, h.p2.x);
            var xs = new HashSet<float> { leftX, rightX };

            foreach (var v in verticals)
            {
                float vx = v.p1.x;
                float vMinY = Mathf.Min(v.p1.y, v.p2.y);
                float vMaxY = Mathf.Max(v.p1.y, v.p2.y);

                if (vx > leftX + 1e-5f && vx < rightX - 1e-5f &&
                    y > vMinY + 1e-5f && y < vMaxY - 1e-5f)
                {
                    xs.Add(vx);
                }
            }

            var sortedXs = new List<float>(xs);
            sortedXs.Sort();

            for (int i = 0; i < sortedXs.Count - 1; i++)
            {
                Vector2 a = new Vector2(sortedXs[i], y);
                Vector2 b = new Vector2(sortedXs[i + 1], y);
                output.Add(new Edge(a, b));
            }
        }

        // Split verticals by all horizontals
        foreach (var v in verticals)
        {
            float x = v.p1.x;
            float bottomY = Mathf.Min(v.p1.y, v.p2.y);
            float topY = Mathf.Max(v.p1.y, v.p2.y);
            var ys = new HashSet<float> { bottomY, topY };

            foreach (var h in horizontals)
            {
                float hy = h.p1.y;
                float hMinX = Mathf.Min(h.p1.x, h.p2.x);
                float hMaxX = Mathf.Max(h.p1.x, h.p2.x);

                if (hy > bottomY + 1e-5f && hy < topY - 1e-5f &&
                    x > hMinX + 1e-5f && x < hMaxX - 1e-5f)
                {
                    ys.Add(hy);
                }
            }

            var sortedYs = new List<float>(ys);
            sortedYs.Sort();

            for (int i = 0; i < sortedYs.Count - 1; i++)
            {
                Vector2 a = new Vector2(x, sortedYs[i]);
                Vector2 b = new Vector2(x, sortedYs[i + 1]);
                output.Add(new Edge(a, b));
            }
        }

        return output;
    }
}
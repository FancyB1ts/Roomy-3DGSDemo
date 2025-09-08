using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class FloorplanLoader : MonoBehaviour
{
    [Header("Labelme JSON")]
    public TextAsset jsonFile;

    [Header("Settings")]
    public float wallHeight = 2.5f;

    [Header("Materials")]
    public Material wallMaterial;
    public Material floorMaterial;

    [Header("Debug")]
    public bool updateEveryFrame = false;

    private float lastUpdateTime;

    void Start()
    {
        GenerateFromJson();
    }

    void Update()
    {
        if (updateEveryFrame && Time.time - lastUpdateTime > 0.1f)
        {
            GenerateFromJson();
            lastUpdateTime = Time.time;
        }
    }

    [ContextMenu("Update Floorplan")]
    public void GenerateFromJson()
    {
        // Destroy previous geometry
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        if (jsonFile == null)
        {
            Debug.LogError("No JSON file assigned.");
            return;
        }

        var data = JsonConvert.DeserializeObject<LabelmeData>(jsonFile.text);

        Debug.Log($"[DEBUG] Shapes in JSON: {data.shapes.Count}");

        // --- Floor ---
        var floorShape = data.shapes.FirstOrDefault(s => s.label == "area");
        if (floorShape != null)
        {
            Debug.Log("[DEBUG] Floor shape found.");
            var floorObj = BuildFloor(floorShape.points);
            floorObj.name = "Floor";
            floorObj.transform.SetParent(transform);
        }
        else
        {
            Debug.LogWarning("[DEBUG] No floor shape found.");
        }

        // --- Walls ---
        var wallRects = data.shapes
            .Where(s => s.label == "wall" && s.shape_type == "rectangle")
            .Select(s => s.points)
            .ToList();

        Debug.Log($"[DEBUG] Wall rectangles found: {wallRects.Count}");
        for (int i = 0; i < wallRects.Count; i++)
            Debug.Log($"[DEBUG] Wall rect #{i}: {JsonConvert.SerializeObject(wallRects[i])}");

        var wallObj = BuildWallsFromRectangles(wallRects, wallHeight);
        wallObj.name = "Walls";
        wallObj.transform.SetParent(transform);
    }

    GameObject BuildFloor(List<List<float>> points)
    {
        Vector2 p1 = new Vector2(points[0][0], points[0][1]);
        Vector2 p2 = new Vector2(points[1][0], points[1][1]);

        float xMin = Mathf.Min(p1.x, p2.x);
        float xMax = Mathf.Max(p1.x, p2.x);
        float yMin = Mathf.Min(p1.y, p2.y);
        float yMax = Mathf.Max(p1.y, p2.y);

        Vector3[] verts = new Vector3[]
        {
            new Vector3(xMin, 0, yMin),
            new Vector3(xMax, 0, yMin),
            new Vector3(xMax, 0, yMax),
            new Vector3(xMin, 0, yMax)
        };

        int[] tris = new int[] { 0, 2, 1, 0, 3, 2 };

        var quad = new GameObject("Floor");
        var mf = quad.AddComponent<MeshFilter>();
        var mr = quad.AddComponent<MeshRenderer>();
        var mesh = new Mesh();

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mf.mesh = mesh;

        mr.material = floorMaterial != null ? floorMaterial : new Material(Shader.Find("Standard"));

        quad.transform.localScale = new Vector3(1, 1, -1); // mirror on Z

        return quad;
    }

    GameObject BuildWallsFromRectangles(List<List<List<float>>> rectangles, float height)
    {
        var edges = new List<Edge>();
        var vertices = new HashSet<Vector2>();

        int degenerateCount = 0;
        int processedCount = 0;

        foreach (var rect in rectangles)
        {
            Vector2 a = new Vector2(rect[0][0], rect[0][1]);
            Vector2 b = new Vector2(rect[1][0], rect[1][1]);

            float xMin = Mathf.Min(a.x, b.x);
            float xMax = Mathf.Max(a.x, b.x);
            float yMin = Mathf.Min(a.y, b.y);
            float yMax = Mathf.Max(a.y, b.y);

            // skip degenerate
            if (Mathf.Approximately(xMin, xMax) || Mathf.Approximately(yMin, yMax))
            {
                Debug.LogWarning($"[DEBUG] Degenerate wall rectangle skipped: {JsonConvert.SerializeObject(rect)}");
                degenerateCount++;
                continue;
            }

            processedCount++;
            Debug.Log($"[DEBUG] Wall rect processed: a={a}, b={b}, corners=({xMin},{yMin})-({xMax},{yMax})");

            Vector2[] corners = new Vector2[]
            {
                new Vector2(xMin, yMin),
                new Vector2(xMax, yMin),
                new Vector2(xMax, yMax),
                new Vector2(xMin, yMax)
            };

            for (int i = 0; i < 4; i++)
            {
                Edge e = new Edge(corners[i], corners[(i + 1) % 4]);
                edges.Add(e);
                vertices.Add(e.a);
                vertices.Add(e.b);
            }
        }

        Debug.Log($"[DEBUG] Wall rectangles processed: {processedCount}, degenerate skipped: {degenerateCount}");

        var splitEdges = SplitEdgesAtIntersections(edges);
        Debug.Log($"[DEBUG] Split edges count: {splitEdges.Count}");

        var loops = FindClosedLoops(splitEdges);
        Debug.Log($"[DEBUG] Closed loops found: {loops.Count}");
        for (int i = 0; i < loops.Count; i++)
            Debug.Log($"[DEBUG] Loop #{i}: {string.Join(", ", loops[i].Select(v => v.ToString()))}");

        GameObject root = new GameObject("Walls");

        for (int i = 0; i < loops.Count; i++)
        {
            var loop = loops[i];

            if (loop.Count < 3)
            {
                Debug.LogWarning($"[DEBUG] Skipping loop with fewer than 3 points: {loop.Count}");
                continue;
            }

            Debug.Log($"[DEBUG] Triangulating loop #{i} with {loop.Count} points.");

            var mesh = TriangulateAndExtrude(loop, height);
            if (mesh == null)
            {
                Debug.LogWarning($"[DEBUG] Triangulation failed for loop #{i}.");
                continue;
            }
            mesh.transform.SetParent(root.transform);
        }

        return root;
    }

    GameObject TriangulateAndExtrude(List<Vector2> loop, float height)
    {
        if (loop.Count < 3)
        {
            Debug.LogWarning("[DEBUG] Triangulation skipped, fewer than 3 points.");
            return null;
        }

        Vector3[] baseVerts = loop.Select(p => new Vector3(p.x, 0, p.y)).ToArray();
        int[] tris = new int[(baseVerts.Length - 2) * 3];

        for (int i = 0; i < baseVerts.Length - 2; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        Vector3[] fullVerts = new Vector3[baseVerts.Length * 2];
        for (int i = 0; i < baseVerts.Length; i++)
        {
            fullVerts[i] = baseVerts[i];
            fullVerts[i + baseVerts.Length] = baseVerts[i] + Vector3.up * height;
        }

        List<int> allTris = new List<int>(tris);

        int offset = baseVerts.Length;
        for (int i = 0; i < baseVerts.Length - 2; i++)
        {
            allTris.Add(offset);
            allTris.Add(offset + i + 2);
            allTris.Add(offset + i + 1);
        }

        for (int i = 0; i < baseVerts.Length; i++)
        {
            int next = (i + 1) % baseVerts.Length;
            int a = i;
            int b = next;
            int c = i + baseVerts.Length;
            int d = next + baseVerts.Length;

            allTris.AddRange(new int[] { a, c, d, a, d, b });
        }

        GameObject wall = new GameObject("WallMesh");
        var mf = wall.AddComponent<MeshFilter>();
        var mr = wall.AddComponent<MeshRenderer>();
        var mesh = new Mesh();

        mesh.vertices = fullVerts;
        mesh.triangles = allTris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mf.mesh = mesh;

        mr.material = wallMaterial != null ? wallMaterial : new Material(Shader.Find("Standard"));
        wall.transform.localScale = new Vector3(1, 1, -1); // mirror on Z

        Debug.Log($"[DEBUG] Created wall mesh with {fullVerts.Length} vertices, {allTris.Count / 3} triangles");

        return wall;
    }

    List<Edge> SplitEdgesAtIntersections(List<Edge> edges)
    {
        List<Edge> result = new List<Edge>();

        for (int i = 0; i < edges.Count; i++)
        {
            var e1 = edges[i];
            List<Vector2> splitPoints = new() { e1.a, e1.b };

            for (int j = 0; j < edges.Count; j++)
            {
                if (i == j) continue;
                var e2 = edges[j];
                if (LineSegmentsIntersect(e1.a, e1.b, e2.a, e2.b, out Vector2 p))
                {
                    if (!splitPoints.Contains(p))
                        splitPoints.Add(p);
                }
            }

            splitPoints = splitPoints.OrderBy(p => Vector2.Distance(e1.a, p)).ToList();
            for (int k = 0; k < splitPoints.Count - 1; k++)
                result.Add(new Edge(splitPoints[k], splitPoints[k + 1]));
        }

        return result;
    }

    List<List<Vector2>> FindClosedLoops(List<Edge> edges)
    {
        var loops = new List<List<Vector2>>();
        var edgeDict = edges.GroupBy(e => e.a).ToDictionary(g => g.Key, g => g.ToList());
        var visited = new HashSet<(Vector2, Vector2)>();

        foreach (var edge in edges)
        {
            if (visited.Contains((edge.a, edge.b)) || visited.Contains((edge.b, edge.a)))
                continue;

            var loop = new List<Vector2> { edge.a };
            Vector2 current = edge.b;
            Vector2 prev = edge.a;

            while (current != edge.a)
            {
                loop.Add(current);
                visited.Add((prev, current));

                if (!edgeDict.ContainsKey(current)) break;

                var nextEdges = edgeDict[current]
                    .Where(e => !visited.Contains((current, e.b)) && e.b != prev)
                    .ToList();

                if (nextEdges.Count == 0) break;

                prev = current;
                current = nextEdges[0].b;
            }

            if (loop.Count >= 3 && current == edge.a)
                loops.Add(loop);
        }

        return loops;
    }

    bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        float A1 = p2.y - p1.y;
        float B1 = p1.x - p2.x;
        float C1 = A1 * p1.x + B1 * p1.y;

        float A2 = q2.y - q1.y;
        float B2 = q1.x - q2.x;
        float C2 = A2 * q1.x + B2 * q1.y;

        float det = A1 * B2 - A2 * B1;
        if (Mathf.Abs(det) < 0.001f)
            return false;

        float x = (B2 * C1 - B1 * C2) / det;
        float y = (A1 * C2 - A2 * C1) / det;
        intersection = new Vector2(x, y);

        return IsBetween(p1, p2, intersection) && IsBetween(q1, q2, intersection);
    }

    bool IsBetween(Vector2 a, Vector2 b, Vector2 p)
    {
        return Mathf.Min(a.x, b.x) - 0.01f <= p.x && p.x <= Mathf.Max(a.x, b.x) + 0.01f &&
               Mathf.Min(a.y, b.y) - 0.01f <= p.y && p.y <= Mathf.Max(a.y, b.y) + 0.01f;
    }

    public struct Edge
    {
        public Vector2 a;
        public Vector2 b;
        public Edge(Vector2 a, Vector2 b) { this.a = a; this.b = b; }
    }

    [System.Serializable]
    public class LabelmeData
    {
        public List<Shape> shapes;
    }

    [System.Serializable]
    public class Shape
    {
        public string label;
        public List<List<float>> points;
        public string shape_type;
    }
}
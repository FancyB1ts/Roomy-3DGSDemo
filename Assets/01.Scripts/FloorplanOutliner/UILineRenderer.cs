using UnityEngine;
using UnityEngine.UI;

namespace Radishmouse
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : MaskableGraphic
    {
        [Header("Line Settings")]
        public Vector2[] points = new Vector2[0];
        public float thickness = 10f;
        public bool center = true;
        
        [Header("Polygon Settings")]
        public bool closedPolygon = false;
        public bool smoothCorners = true;
        
        [Header("Performance")]
        public bool optimizeForStaticLines = false;
        
        // Cache for performance
        private Vector2[] cachedPoints;
        private bool meshDirty = true;
        
        // Canvas reference for scale normalization
        private Canvas canvas;
        
        protected override void Awake()
        {
            base.Awake();
            canvas = GetComponentInParent<Canvas>();
        }
        
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            
            if (points == null || points.Length < 2)
                return;
            
            // Performance optimization - only rebuild if points changed
            if (optimizeForStaticLines && !meshDirty && 
                cachedPoints != null && ArraysEqual(points, cachedPoints))
                return;
            
            if (closedPolygon && points.Length >= 3)
            {
                CreateClosedPolygon(vh);
            }
            else
            {
                CreateOpenLine(vh);
            }
            
            // Cache current points
            if (optimizeForStaticLines)
            {
                CachePoints();
                meshDirty = false;
            }
        }
        
        private void CreateOpenLine(VertexHelper vh)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                int index = vh.currentVertCount;
                CreateLineSegment(points[i], points[i + 1], vh);
                
                // Correct triangle indices for a proper quad
                // Triangle 1: bottom-left, top-left, top-right
                vh.AddTriangle(index, index + 1, index + 2);
                // Triangle 2: bottom-left, top-right, bottom-right  
                vh.AddTriangle(index, index + 2, index + 3);
                
                // Smooth corners between segments
                if (smoothCorners && i > 0)
                {
                    CreateCornerJoin(points[i - 1], points[i], points[i + 1], vh);
                }
            }
        }
        
        private void CreateClosedPolygon(VertexHelper vh)
        {
            int pointCount = points.Length;
            
            for (int i = 0; i < pointCount; i++)
            {
                int nextIndex = (i + 1) % pointCount;
                int index = vh.currentVertCount;
                CreateLineSegment(points[i], points[nextIndex], vh);
                
                // Correct triangle indices for a proper quad
                vh.AddTriangle(index, index + 1, index + 2);
                vh.AddTriangle(index, index + 2, index + 3);
            }
            
            // Create corner joins for closed polygon
            if (smoothCorners)
            {
                for (int i = 0; i < pointCount; i++)
                {
                    int prevIndex = (i - 1 + pointCount) % pointCount;
                    int nextIndex = (i + 1) % pointCount;
                    CreateCornerJoin(points[prevIndex], points[i], points[nextIndex], vh);
                }
            }
        }
        
        private void CreateLineSegment(Vector2 point1, Vector2 point2, VertexHelper vh)
        {
            // Calculate direction and perpendicular for consistent thickness
            Vector2 direction = (point2 - point1);
            if (direction.magnitude < 0.01f) return; // Skip zero-length segments
            
            direction = direction.normalized;
            
            // FIX: Apply canvas scale normalization to thickness
            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            float halfThickness = (thickness * scaleFactor) * 0.5f;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * halfThickness;
            
            Color32 c = color;
            
            // Bottom-left
            UIVertex bl = UIVertex.simpleVert;
            bl.color = c;
            bl.position = new Vector3(point1.x - perpendicular.x, point1.y - perpendicular.y, 0);
            bl.uv0 = new Vector2(0, 0);
            vh.AddVert(bl);
            
            // Top-left
            UIVertex tl = UIVertex.simpleVert;
            tl.color = c;
            tl.position = new Vector3(point1.x + perpendicular.x, point1.y + perpendicular.y, 0);
            tl.uv0 = new Vector2(0, 1);
            vh.AddVert(tl);
            
            // Top-right
            UIVertex tr = UIVertex.simpleVert;
            tr.color = c;
            tr.position = new Vector3(point2.x + perpendicular.x, point2.y + perpendicular.y, 0);
            tr.uv0 = new Vector2(1, 1);
            vh.AddVert(tr);
            
            // Bottom-right
            UIVertex br = UIVertex.simpleVert;
            br.color = c;
            br.position = new Vector3(point2.x - perpendicular.x, point2.y - perpendicular.y, 0);
            br.uv0 = new Vector2(1, 0);
            vh.AddVert(br);
        }
        
        private void CreateCornerJoin(Vector2 prevPoint, Vector2 currentPoint, Vector2 nextPoint, VertexHelper vh)
        {
            // Calculate angles for smooth corner joining
            Vector2 dir1 = (currentPoint - prevPoint);
            Vector2 dir2 = (nextPoint - currentPoint);
            
            // Skip zero-length segments
            if (dir1.magnitude < 0.01f || dir2.magnitude < 0.01f) return;
            
            dir1 = dir1.normalized;
            dir2 = dir2.normalized;
            
            // Skip if directions are too similar (straight line)
            float angle = Vector2.Angle(dir1, dir2);
            if (angle < 5f || angle > 175f) return;
            
            Color32 c = color;
            
            // FIX: Apply canvas scale normalization to corner thickness
            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            float halfThickness = (thickness * scaleFactor) * 0.5f;
            
            // Create small triangle to fill corner gap with consistent thickness
            Vector2 perp1 = new Vector2(-dir1.y, dir1.x) * halfThickness;
            Vector2 perp2 = new Vector2(-dir2.y, dir2.x) * halfThickness;
            
            // Center vertex
            UIVertex center = UIVertex.simpleVert;
            center.color = c;
            center.position = new Vector3(currentPoint.x, currentPoint.y, 0);
            vh.AddVert(center);
            
            // First perpendicular vertex
            UIVertex perp1Vert = UIVertex.simpleVert;
            perp1Vert.color = c;
            perp1Vert.position = new Vector3(currentPoint.x + perp1.x, currentPoint.y + perp1.y, 0);
            vh.AddVert(perp1Vert);
            
            // Second perpendicular vertex
            UIVertex perp2Vert = UIVertex.simpleVert;
            perp2Vert.color = c;
            perp2Vert.position = new Vector3(currentPoint.x + perp2.x, currentPoint.y + perp2.y, 0);
            vh.AddVert(perp2Vert);
            
            // Add triangle
            int vertCount = vh.currentVertCount;
            vh.AddTriangle(vertCount - 3, vertCount - 2, vertCount - 1);
        }
        
        // Public methods for dynamic updates
        public void SetPoints(Vector2[] newPoints, bool isClosedPolygon = false)
        {
            points = newPoints;
            closedPolygon = isClosedPolygon;
            MarkMeshDirty();
        }
        
        public void AddPoint(Vector2 point)
        {
            System.Array.Resize(ref points, points.Length + 1);
            points[points.Length - 1] = point;
            MarkMeshDirty();
        }
        
        public void SetThickness(float newThickness)
        {
            thickness = newThickness;
            MarkMeshDirty();
        }
        
        public void MarkMeshDirty()
        {
            meshDirty = true;
            SetVerticesDirty();
        }
        
        // Utility methods
        private void CachePoints()
        {
            if (cachedPoints == null || cachedPoints.Length != points.Length)
                cachedPoints = new Vector2[points.Length];
            
            System.Array.Copy(points, cachedPoints, points.Length);
        }
        
        private bool ArraysEqual(Vector2[] arr1, Vector2[] arr2)
        {
            if (arr1.Length != arr2.Length)
                return false;
            
            for (int i = 0; i < arr1.Length; i++)
            {
                if (Vector2.Distance(arr1[i], arr2[i]) > 0.001f)
                    return false;
            }
            return true;
        }
        
        // Override for better property integration
        public override Color color
        {
            get { return base.color; }
            set
            {
                if (base.color != value)
                {
                    base.color = value;
                    MarkMeshDirty();
                }
            }
        }
        
#if UNITY_EDITOR
        // Editor helpers
        protected override void OnValidate()
        {
            base.OnValidate();
            MarkMeshDirty();
        }
#endif
    }
}
// FurnitureDragNoPhysics: Drag-and-drop with refined push-aside and correct Y handling
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FurnitureDragNoPhysics : MonoBehaviour
{
    [Header("Camera & Layers")]
    public Camera topDownCamera;
    public LayerMask furnitureMask;

    [Header("UI")]
    public RectTransform dragArea;

    [Header("Floor plane height")]
    public float planeY = 0f;

    [Header("Bounds (world)")]
    public bool useBounds = true;
    private float minX, maxX, minZ, maxZ;

    [Header("Push Aside settings")]
    public bool pushEnabled = true;
    public float pushDuration = 0.2f;

    private BoxCollider pickedCollider;
    private Vector3 pickOffset;
    private List<BoxCollider> allFurniture;
    private Dictionary<Transform, Vector3> originalPositions;
    private HashSet<Transform> pushed;
    private bool warnedMissingDragArea;

    void Start()
    {
        allFurniture = new List<BoxCollider>(FindObjectsOfType<BoxCollider>());
        originalPositions = new Dictionary<Transform, Vector3>();
        pushed = new HashSet<Transform>();
        foreach (var col in allFurniture)
            originalPositions[col.transform] = col.transform.position;

        if (useBounds) CalculateBounds();
    }

    void CalculateBounds()
    {
        float camH = topDownCamera.transform.position.y;
        float dist = camH - planeY;
        Vector3 bl = topDownCamera.ViewportToWorldPoint(new Vector3(0f, 0f, dist));
        Vector3 tr = topDownCamera.ViewportToWorldPoint(new Vector3(1f, 1f, dist));
        minX = bl.x; maxX = tr.x;
        minZ = bl.z; maxZ = tr.z;
    }

    void Update()
    {
        // Pick up
        if (Input.GetMouseButtonDown(0))
        {
            if (dragArea != null)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(dragArea, Input.mousePosition, null))
                    return;
            }
            else if (!warnedMissingDragArea)
            {
                Debug.LogWarning("dragArea is null, skipping UI check.");
                warnedMissingDragArea = true;
            }

            // Compute world XZ from screen
            Vector3 worldXZ = ScreenToWorldXZ(Input.mousePosition);
            float camH = topDownCamera.transform.position.y;
            Ray ray = new Ray(new Vector3(worldXZ.x, camH, worldXZ.z), Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, furnitureMask))
            {
                pickedCollider = hit.collider as BoxCollider;
                if (pickedCollider != null)
                {
                    // Compute pick offset in XZ plane, keep Y consistent
                    float y0 = planeY;
                    Vector3 hitPoint = new Vector3(hit.point.x, y0, hit.point.z);
                    Vector3 center = new Vector3(pickedCollider.transform.position.x, y0, pickedCollider.transform.position.z);
                    pickOffset = center - hitPoint;
                }
            }
        }

        // Dragging
        if (pickedCollider != null && Input.GetMouseButton(0))
        {
            Vector3 worldXZ = ScreenToWorldXZ(Input.mousePosition);
            Vector3 targetXZ = worldXZ + new Vector3(pickOffset.x, 0f, pickOffset.z);

            // Clamp within bounds
            if (useBounds)
            {
                var ext = pickedCollider.bounds.extents;
                targetXZ.x = Mathf.Clamp(targetXZ.x, minX + ext.x, maxX - ext.x);
                targetXZ.z = Mathf.Clamp(targetXZ.z, minZ + ext.z, maxZ - ext.z);
            }

            // Collision detection
            Bounds shifted = pickedCollider.bounds;
            shifted.center = new Vector3(targetXZ.x, planeY, targetXZ.z);
            List<Transform> colliding = new List<Transform>();
            foreach (var other in allFurniture)
            {
                if (other == pickedCollider) continue;
                if (other.bounds.Intersects(shifted))
                {
                    colliding.Add(other.transform);
                    if (pushEnabled && !pushed.Contains(other.transform))
                    {
                        StartCoroutine(PushAside(other.transform, targetXZ));
                        pushed.Add(other.transform);
                    }
                }
            }

            // Return freed ones not overlapped and not blocked by active
            if (pushEnabled)
            {
                var toReturn = new List<Transform>();
                foreach (var tr in pushed)
                {
                    if (!colliding.Contains(tr) && !IsActiveOverOriginal(tr, targetXZ))
                        toReturn.Add(tr);
                }
                foreach (var tr in toReturn)
                {
                    StartCoroutine(ReturnToOriginal(tr));
                    pushed.Remove(tr);
                }
            }

            // Apply position at planeY
            pickedCollider.transform.position = new Vector3(targetXZ.x, planeY, targetXZ.z);
        }

        // Drop
        if (Input.GetMouseButtonUp(0) && pickedCollider != null)
        {
            if (pushEnabled)
            {
                Vector3 targetXZ = new Vector3(pickedCollider.transform.position.x, 0f, pickedCollider.transform.position.z);
                var toReturn = new List<Transform>(pushed);
                foreach (var tr in toReturn)
                {
                    if (!IsActiveOverOriginal(tr, targetXZ))
                    {
                        StartCoroutine(ReturnToOriginal(tr));
                        pushed.Remove(tr);
                    }
                }
            }
            pickedCollider = null;
        }
    }

    // Map screen point to world XZ on planeY
    private Vector3 ScreenToWorldXZ(Vector2 screenPt)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(dragArea, screenPt, null, out Vector2 local);
        Vector2 size = dragArea.rect.size;
        Vector2 uv = new Vector2((local.x + size.x * 0.5f) / size.x, (local.y + size.y * 0.5f) / size.y);
        float x = Mathf.Lerp(minX, maxX, uv.x);
        float z = Mathf.Lerp(minZ, maxZ, uv.y);
        return new Vector3(x, planeY, z);
    }

    // Check if active is over other's original spot (XZ)
    private bool IsActiveOverOriginal(Transform other, Vector3 targetXZ)
    {
        Vector3 orig = originalPositions[other];
        var ext = other.GetComponent<BoxCollider>().bounds.extents;
        Bounds b = new Bounds(new Vector3(orig.x, planeY, orig.z), new Vector3(ext.x * 2f, 0f, ext.z * 2f));
        return b.Contains(new Vector3(targetXZ.x, planeY, targetXZ.z));
    }

    IEnumerator PushAside(Transform other, Vector3 targetXZ)
    {
        Vector3 dir = (other.position - new Vector3(targetXZ.x, planeY, targetXZ.z)).normalized;
        if (dir == Vector3.zero) dir = Vector3.right;
        float dist = pickedCollider.bounds.extents.magnitude + other.GetComponent<BoxCollider>().bounds.extents.magnitude;
        Vector3 dest = other.position + new Vector3(dir.x, 0f, dir.z) * dist;
        if (useBounds)
        {
            var ext = other.GetComponent<BoxCollider>().bounds.extents;
            dest.x = Mathf.Clamp(dest.x, minX + ext.x, maxX - ext.x);
            dest.z = Mathf.Clamp(dest.z, minZ + ext.z, maxZ - ext.z);
        }
        Vector3 start = other.position;
        float t = 0f;
        while (t < pushDuration)
        {
            other.position = Vector3.Lerp(start, dest, t / pushDuration);
            t += Time.deltaTime;
            yield return null;
        }
        other.position = dest;
    }

    IEnumerator ReturnToOriginal(Transform other)
    {
        Vector3 start = other.position;
        Vector3 orig = originalPositions[other];
        Vector3 dest = new Vector3(orig.x, planeY, orig.z);
        float t = 0f;
        while (t < pushDuration)
        {
            other.position = Vector3.Lerp(start, dest, t / pushDuration);
            t += Time.deltaTime;
            yield return null;
        }
        other.position = dest;
    }
}

using UnityEngine;
using System.Collections.Generic;

public class SessionChangeWatcher : MonoBehaviour
{
    [SerializeField] private SessionAutosaveManager autosaveManager;
    [SerializeField] private SessionDataExporter exporter;
    [SerializeField, Min(0.1f)] private float tickInterval = 0.5f;
    [SerializeField, Min(0f)] private float stabilityWindow = 0.5f; // must remain unchanged for this long

    [SerializeField, Min(0.5f)] private float rescanInterval = 2f;
    private float nextRescanTime;
    private readonly HashSet<Transform> trackedRoots = new HashSet<Transform>();

    private struct WatchedItem
    {
        public Transform root;           // PF_F_ or floorplan transform
        public BoxCollider collider;     // SM_ child's BoxCollider (optional)
        public string lastHash;          // combined hash we compare against
        public string pendingHash;       // last observed differing hash (candidate)
        public float stableUntil;        // time when the pendingHash is considered stable
        public bool isFloorplan;         // marks the floorplan item
    }
    
    private readonly List<WatchedItem> items = new List<WatchedItem>(64);
    private bool backupClockStarted = false; // ensure we start it once

    private float nextCheckTime;

    private void Start()
    {
        if (!autosaveManager) autosaveManager = FindObjectOfType<SessionAutosaveManager>();
        if (!exporter) exporter = FindObjectOfType<SessionDataExporter>();
        
        // Floorplan reference from exporter (tracked for transform only)
        var floorplan = exporter != null ? exporter.floorplanPlane : null;
        if (floorplan) 
        {
            TrackObject(floorplan, null, true);
            trackedRoots.Add(floorplan);
        }
        
        // Furniture by name filter: PF_F_*
        foreach (var go in FindObjectsOfType<GameObject>())
        {
            if (!go || !go.name.StartsWith("PF_F_")) continue;
            var root = go.transform;
            var smCollider = FindSMChildBoxCollider(root);
            TrackObject(root, smCollider, false);
            trackedRoots.Add(root);
        }
        
        nextCheckTime = Time.unscaledTime + tickInterval;
        nextRescanTime = Time.unscaledTime + rescanInterval;
    }

    private void TrackObject(Transform root, BoxCollider smCollider, bool isFloorplan)
    {
        if (!root) return;
        var hash = ComputeHash(root, smCollider);
        items.Add(new WatchedItem
        {
            root = root,
            collider = smCollider,
            lastHash = hash,
            pendingHash = null,
            stableUntil = 0f,
            isFloorplan = isFloorplan
        });
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheckTime) return;
        nextCheckTime = Time.unscaledTime + tickInterval;

        if (Time.unscaledTime >= nextRescanTime)
        {
            RescanForNewFurniture();
            nextRescanTime = Time.unscaledTime + rescanInterval;
        }

        float now = Time.unscaledTime;

        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (!it.root)
                continue;

            string current = ComputeHash(it.root, it.collider);

            // If the current state equals the committed lastHash, clear any pending change.
            if (current == it.lastHash)
            {
                it.pendingHash = null;
                it.stableUntil = 0f;
                items[i] = it;
                continue;
            }

            // State differs from committed lastHash.
            if (it.pendingHash == null || it.pendingHash != current)
            {
                // New candidate; start stability window.
                it.pendingHash = current;
                it.stableUntil = now + stabilityWindow;
                items[i] = it;
                continue;
            }

            // Same candidate as last tick; if stable window elapsed, commit and mark dirty.
            if (now >= it.stableUntil)
            {
                it.lastHash = current;
                it.pendingHash = null;
                it.stableUntil = 0f;
                items[i] = it;

                // Mark session dirty once the change is stable
                autosaveManager?.MarkDirty($"Changed: {it.root.name}");

                // Start backup clock the first time the floorplan commits a change
                if (it.isFloorplan && !backupClockStarted)
                {
                    autosaveManager?.StartBackupClock();
                    backupClockStarted = true;
                }
            }
            else
            {
                // Still within stability window; wait.
                items[i] = it;
            }
        }
    }

    private void RescanForNewFurniture()
    {
        foreach (var go in FindObjectsOfType<GameObject>())
        {
            if (!go || !go.name.StartsWith("PF_F_")) continue;
            var root = go.transform;
            if (trackedRoots.Contains(root)) continue;
            var smCollider = FindSMChildBoxCollider(root);
            TrackObject(root, smCollider, false);
            trackedRoots.Add(root);
        }
    }

    private BoxCollider FindSMChildBoxCollider(Transform root)
    {
        if (!root) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child && child.name.StartsWith("SM_"))
            {
                if (child.TryGetComponent<BoxCollider>(out var col))
                    return col;
            }
        }
        // If not found on direct children, optionally search deeper:
        var colliders = root.GetComponentsInChildren<BoxCollider>(true);
        foreach (var col in colliders)
        {
            var t = col.transform;
            if (t && t.name.StartsWith("SM_")) return col;
        }
        return null;
    }

    private string ComputeHash(Transform t, BoxCollider col)
    {
        // Parent transform (local)
        Vector3 p = t.localPosition;
        Quaternion r = t.localRotation;
        Vector3 s = t.localScale;
    
        // Collider (optional)
        if (col)
        {
            Vector3 cCenter = col.center;
            Vector3 cSize = col.size;
            return $"{p.x:F3},{p.y:F3},{p.z:F3}|{r.x:F3},{r.y:F3},{r.z:F3},{r.w:F3}|{s.x:F3},{s.y:F3},{s.z:F3}|C{cCenter.x:F3},{cCenter.y:F3},{cCenter.z:F3}|S{cSize.x:F3},{cSize.y:F3},{cSize.z:F3}";
        }
        else
        {
            return $"{p.x:F3},{p.y:F3},{p.z:F3}|{r.x:F3},{r.y:F3},{r.z:F3},{r.w:F3}|{s.x:F3},{s.y:F3},{s.z:F3}";
        }
    }
}
using UnityEngine;

public class SessionAutosaveManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SessionDataExporter exporter;
    [SerializeField] private SessionDataUploader uploader;

    [Header("Policy")]
    [SerializeField, Min(0.1f)] private float debounceSeconds = 3f;      
    [SerializeField, Min(1f)]   private float minUploadInterval = 15f;
    [SerializeField, Min(5f)]   private float backupInterval = 45f;

    [Header("Mode")]
    [SerializeField] private bool dryRun = true;   // start with logs only; we'll flip later
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private string sessionId;

    // State
    private bool dirty;
    private bool queuedAfterCooldown;
    private float debounceDeadline = -1f;
    private float nextAllowedUploadTime = 0f;
    private float nextBackupTime = 0f;

    private string lastUploadedHash = string.Empty;
    private string cachedLastJson = string.Empty;

    private bool uploadInProgress;   // prevents overlapping uploads within the same frame/tick

    // --- Public API: call this from UI / gameplay when a “done” action occurs ---
    public void MarkDirty(string reason = null)
    {
        dirty = true;
        var now = Time.unscaledTime;

        // NEW: start the backup clock on first edit (prevents immediate backup)
        if (nextBackupTime <= 0f) nextBackupTime = now + backupInterval;

        debounceDeadline = now + debounceSeconds;

        if (enableDebugLogs)
            Debug.Log($"[Autosave] MarkDirty(reason='{reason ?? "n/a"}') → debounce until {debounceDeadline:F2}");
    }

    public void StartBackupClock()
    {
        var now = Time.unscaledTime;
        nextBackupTime = now + backupInterval;
        if (enableDebugLogs)
            Debug.Log($"[Autosave] Backup clock started. First backup @ {nextBackupTime:F2}");
    }

    // Optional: editor testing (press 'K' to mark dirty)
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) MarkDirty("EditorKey[K]");

        Tick();
    }
#else
    private void Update() => Tick();
#endif

    private void Tick()
    {
        float now = Time.unscaledTime;

        // Debounced save
        if (dirty && !uploadInProgress && debounceDeadline > 0f && now >= debounceDeadline)
        {
            TrySaveNow("debounced");
        }

        // Backup save
        if (dirty && !uploadInProgress && now >= nextBackupTime)
        {
            TrySaveNow("backup");
        }

        // Cooldown release
        if (queuedAfterCooldown && !uploadInProgress && now >= nextAllowedUploadTime)
        {
            TrySaveNow("cooldown-release");
        }
    }

    private void TrySaveNow(string reason)
    {
        if (uploadInProgress)
        {
            if (enableDebugLogs)
                Debug.Log($"[Autosave] Save '{reason}' skipped — upload already in progress.");
            return;
        }

        float now = Time.unscaledTime;

        if (now < nextAllowedUploadTime)
        {
            queuedAfterCooldown = true; // try again when cooldown ends
            if (enableDebugLogs)
                Debug.Log($"[Autosave] Save '{reason}' blocked by cooldown. Will retry at {nextAllowedUploadTime:F2}");
            return;
        }

        // 1) Serialize current session
        string json = exporter != null ? exporter.GetCurrentSessionAsJson() : null;
        if (string.IsNullOrEmpty(json))
        {
            if (enableDebugLogs) Debug.LogWarning("[Autosave] Exporter returned empty JSON; skipping.");
            // keep dirty so backup/next attempt can try again
            ScheduleNextWindows(now);
            return;
        }

        // 2) Hash to detect duplicates
        string hash = HashUtil.MD5(json);
        if (hash == lastUploadedHash)
        {
            if (enableDebugLogs)
                Debug.Log($"[Autosave] '{reason}' skipped — content unchanged (hash={hash.Substring(0,8)}).");
            // Even if unchanged, clear dirty so we don't spam backups
            dirty = false;
            cachedLastJson = json;
            ScheduleNextWindows(now);
            return;
        }

        // 3) "Upload" (dry-run first)
        if (dryRun)
        {
            uploadInProgress = true;
            uploader?.DebugEcho(json);
            if (enableDebugLogs)
                Debug.Log($"[Autosave] DRY-RUN '{reason}' → would upload. hash={hash.Substring(0,8)}");
            // Assume success for flow testing
            OnUploadSuccess(now, json, hash);
        }
        else
        {
            // Real upload with completion callback
            uploadInProgress = true;
            if (enableDebugLogs)
                Debug.Log($"[Autosave] '{reason}' → uploading… hash={hash.Substring(0,8)}");

            // Use uploader callback to decide success/failure behavior
            uploader?.Upload(json, success =>
            {
                var t = Time.unscaledTime;
                if (success)
                {
                    OnUploadSuccess(t, json, hash);
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning("[Autosave] Upload failed — will retry on next trigger/backup.");
                    // keep dirty so we retry, but respect min-interval so we don't hammer
                    dirty = true;
                    queuedAfterCooldown = true;
                    ScheduleNextWindows(t);
                    uploadInProgress = false;
                }
            });
        }
    }

    private void OnUploadSuccess(float now, string json, string hash)
    {
        uploadInProgress = false;
        lastUploadedHash = hash;
        cachedLastJson = json;
        dirty = false;
        queuedAfterCooldown = false;
        ScheduleNextWindows(now);

        if (enableDebugLogs)
            Debug.Log($"[Autosave] ✅ Upload recorded. nextAllowed@{nextAllowedUploadTime:F2} backup@{nextBackupTime:F2}");
    }

    private void ScheduleNextWindows(float now)
    {
        nextAllowedUploadTime = now + minUploadInterval;
        nextBackupTime = now + backupInterval;
        debounceDeadline = -1f;
    }

    // Step 3 will call this from JS on exit
    public void FinalSaveRequested()
    {
        // If we have a cached JSON and time is tight, we'd ship that.
        // For Step 1 we just log; wiring to JS comes in Step 3.
        if (enableDebugLogs)
            Debug.Log("[Autosave] FinalSaveRequested() — JS lifecycle not wired yet (Step 3).");
        // You could force a save now if needed:
        // MarkDirty("final"); debounceDeadline = 0f; TrySaveNow("final");
    }

    public string SessionId => sessionId;

    /// <summary>
    /// Returns the most recently cached JSON if available, otherwise tries to get the current session as JSON quickly.
    /// </summary>
    public string TryGetLatestJsonFast()
    {
        if (!string.IsNullOrEmpty(cachedLastJson))
            return cachedLastJson;
        if (exporter != null)
            return exporter.GetCurrentSessionAsJson();
        return null;
    }

}
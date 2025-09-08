// JS bridge for WebGL cookie refresh
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SessionDataUploader : MonoBehaviour
{
    public static SessionDataUploader Instance { get; private set; }

    [Header("Backend Config")]
    [SerializeField] private string uploadUrl = "/.netlify/functions/upload-session";

    [SerializeField] private SessionDataExporter exporter;

    [Header("Identity")]
    [SerializeField] private string userId = "";  // persisted anonymous id if empty
    private const string kUserIdPrefsKey = "roomy_user_id";

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true; // disable to mute non-error logs

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void Roomy_RefreshCookie();
#endif

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // Ensure we have a stable userId (anonymous if not provided)
        if (string.IsNullOrEmpty(userId))
        {
            userId = PlayerPrefs.GetString(kUserIdPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(userId))
            {
                userId = "anon_" + System.Guid.NewGuid().ToString("N").Substring(0, 12);
                PlayerPrefs.SetString(kUserIdPrefsKey, userId);
                PlayerPrefs.Save();
            }
        }
    }

    // Legacy entry point (no completion callback)
    public void Upload(string jsonString)
    {
        Upload(jsonString, null);
    }

    // Overload with completion callback for AutosaveManager
    public void Upload(string jsonString, System.Action<bool> onDone)
    {
        StartCoroutine(UploadCoroutine(jsonString, onDone));
    }

    public void OnSaveSessionButtonClicked()
    {
        if (exporter == null)
        {
            Debug.LogError("[SessionDataUploader] Exporter reference is NULL.");
            return;
        }

        string json = exporter.GetCurrentSessionAsJson();

        // Quick stats before upload (using DebugOverlay helpers)
        // int furnitureCount = DebugOverlay.TryCountFurniture(json);
        // int floorplanLen = DebugOverlay.TryGetFloorplanBase64Length(json);
        // Debug.Log($"[UPL] furnitureCount={furnitureCount}, floorplanBase64Len={floorplanLen}, jsonLen={json?.Length ?? 0}");

        Upload(json);
    }

    private static string ExtractSessionId(string json)
    {
        if (string.IsNullOrEmpty(json)) return string.Empty;
        // very lightweight scan to avoid full JSON parse
        const string key = "\"sessionId\"";
        int k = json.IndexOf(key, System.StringComparison.Ordinal);
        if (k < 0) return string.Empty;
        int colon = json.IndexOf(':', k);
        if (colon < 0) return string.Empty;
        int firstQuote = json.IndexOf('"', colon + 1);
        if (firstQuote < 0) return string.Empty;
        int secondQuote = json.IndexOf('"', firstQuote + 1);
        if (secondQuote < 0) return string.Empty;
        return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
    }

    private IEnumerator UploadCoroutine(string jsonString, System.Action<bool> onDone)
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString ?? "");
        string sessionId = ExtractSessionId(jsonString);

        // Attempt up to 2 tries: initial + one retry on 403 after refreshing cookie
        for (int attempt = 0; attempt < 2; attempt++)
        {
            using (var request = new UnityWebRequest(uploadUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Optional hardening headers
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Expect", ""); // avoid 100-continue issues

                // Identify user and session for one-file-per-session overwrite on the backend
                if (!string.IsNullOrEmpty(userId)) request.SetRequestHeader("X-User-Id", userId);
                if (!string.IsNullOrEmpty(sessionId)) request.SetRequestHeader("X-Session-Id", sessionId);
                request.SetRequestHeader("X-Overwrite", "true"); // signal upsert/overwrite

                Log("[SessionDataUploader] Uploading session data..." + (attempt == 1 ? " (retry)" : string.Empty));

                yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool success = request.result == UnityWebRequest.Result.Success
                               && (request.responseCode >= 200 && request.responseCode < 300);
#else
                bool success = !request.isNetworkError && !request.isHttpError;
#endif

                if (success)
                {
                    Log("[SessionDataUploader] Upload succeeded." + (attempt == 1 ? " (after retry)" : string.Empty));
                    onDone?.Invoke(true);
                    yield break;
                }

                // If unauthorized due to expired cookie, refresh once and retry
                if (attempt == 0 && request.responseCode == 403)
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    try { Roomy_RefreshCookie(); } catch { }
                    // Give the browser a short moment to complete the fetch
                    yield return new WaitForSecondsRealtime(0.25f);
                    continue; // go to next attempt
#else
                    // In Editor or non-WebGL, don't retry (no cookie path)
#endif
                }

                // Final failure path
                Debug.LogError($"[SessionDataUploader] Upload failed {request.responseCode}: {request.error}\n{request.downloadHandler.text}");
                onDone?.Invoke(false);
                yield break;
            }
        }
    }

    // Inside your SessionDataUploader class, anywhere after your other methods:
    public void DebugEcho(string json)
    {
        Log($"[DBG] Uploader would send JSON length={json?.Length ?? 0}");
        if (!string.IsNullOrEmpty(json))
        {
            var preview = json.Length > 600 ? json.Substring(0, 600) : json;
            Log("[DBG] JSON preview: " + preview);
        }
    }

    public string UploadUrl => uploadUrl;
    public string UserId => userId;

    private void Log(string message)
    {
        if (enableDebugLogs) Debug.Log(message);
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs) Debug.LogWarning(message);
    }

    private void LogError(string message)
    {
        if (enableDebugLogs) Debug.LogError(message);
    }
}
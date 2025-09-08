#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

public class WebGLLifecycleBridge : MonoBehaviour
{
    [SerializeField] private SessionAutosaveManager autosave;
    [SerializeField] private SessionDataUploader uploader; // for url + userId access

    // Helper to sanitize IDs for safe embedding in JSON
    private static string SanitizeId(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        // Remove CR/LF and quotes, trim spaces, and allow only a safe subset
        var s = value.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
        s = s.Replace("\"", string.Empty);
        // Whitelist characters: letters, digits, dash, underscore
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        foreach (char c in s)
        {
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_')
                sb.Append(c);
        }
        // hard cap to 128 chars
        if (sb.Length > 128) sb.Length = 128;
        return sb.ToString();
    }

    // Ensure the JSON body includes userId & sessionId (needed for sendBeacon which can't send headers)
    private static string InjectIdsIntoJson(string json, string userId, string sessionId)
    {
        if (string.IsNullOrEmpty(json)) return json;
        // Sanitize inputs
        string uid = SanitizeId(userId);
        string sid = SanitizeId(sessionId);

        // If payload already mentions both keys, leave it as-is (cheap check)
        bool hasUid = json.IndexOf("\"userId\"", System.StringComparison.Ordinal) >= 0;
        bool hasSid = json.IndexOf("\"sessionId\"", System.StringComparison.Ordinal) >= 0;
        if (hasUid && hasSid) return json;

        if (json.Length > 1 && json[0] == '{')
        {
            // Insert missing fields at the front of the object
            string prefix = "{";
            if (!hasUid) prefix += "\"userId\":\"" + uid + "\",";
            if (!hasSid) prefix += "\"sessionId\":\"" + sid + "\",";
            if (prefix.Length == 1) return json; // nothing to add
            return prefix + json.Substring(1);
        }
        // Fallback: wrap non-object payloads
        return "{\"userId\":\"" + uid + "\",\"sessionId\":\"" + sid + "\",\"data\":" + json + "}";
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void Roomy_InitLifecycleHandlers(string goName, string methodName);
    [DllImport("__Internal")] private static extern int  Roomy_SendFinalBeacon(string url, string json, string userId, string sessionId);
#endif

    private void Awake()
    {
        if (!autosave) autosave = FindObjectOfType<SessionAutosaveManager>();
        if (!uploader) uploader = FindObjectOfType<SessionDataUploader>();
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Register to get pagehide/hidden/freeze callbacks
        Roomy_InitLifecycleHandlers(gameObject.name, nameof(OnBrowserExitEvent));
#endif
    }

    // Called by JS on visibility hidden / pagehide / freeze
    public void OnBrowserExitEvent()
    {
        if (autosave == null || uploader == null) return;

        // Ask autosave for the freshest JSON: if dirty and cheap serialize is possible, do it;
        // otherwise use its cached last JSON.
        string json = autosave.TryGetLatestJsonFast(); // see tiny method below
        if (string.IsNullOrEmpty(json)) return;

        // Pull endpoint + ids
        // Endpoint is forced to same-origin inside RoomyFinalSave.jslib; pass empty URL to avoid ambiguity
        string url = string.Empty;
        string userId = uploader.UserId;     // read-only property
        string sessionId = autosave.SessionId; // expose in autosave or read from JSON

        // Merge ids into body so backend can build the key even if headers are dropped by sendBeacon
        string jsonWithIds = InjectIdsIntoJson(json, userId, sessionId);

#if UNITY_WEBGL && !UNITY_EDITOR
        Roomy_SendFinalBeacon(url, jsonWithIds, userId ?? "", sessionId ?? "");
#endif
    }
}
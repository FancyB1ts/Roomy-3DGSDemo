using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using TMPro;

[Serializable]
public class TimedLabel {
    public TMP_Text text;
    [Tooltip("Highlight start time in seconds")] public float start = 0f;
    [Tooltip("Highlight end time in seconds")]   public float end = 1f;
}

/// <summary>
/// Drives color of a set of TMP labels based on a time source (VideoPlayer or manual time),
/// with smooth transitions between inactive<->active states.
/// </summary>
public class TMPColorController : MonoBehaviour {
    [Header("Time Source")]
    public VideoPlayer videoPlayer;          // Optional. If null, use manual/Unity time.
    public bool useManualTime = false;       // If true, use manualTime (set from another script).
    public float manualTime = 0f;            // Seconds, set externally if desired.

    [Header("Labels & Colors")]
    public List<TimedLabel> labels = new List<TimedLabel>();
    public Color baseColor = new Color(0.62f, 0.62f, 0.62f, 1f); // gray
    public Color highlightColor = Color.black;

    [Header("Transitions")]
    [Tooltip("Seconds to fade from base->highlight")] public float fadeInDuration = 0.25f;
    [Tooltip("Seconds to fade from highlight->base")] public float fadeOutDuration = 0.25f;
    [Tooltip("Easing for both fade directions")] public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Options")]
    [Tooltip("If true, no label is highlighted when time is outside all ranges")]
    public bool highlightNoneWhenNoMatch = true;

    // --- runtime state ---
    private int _lastActiveIndex = -999;

    // Per-label transition state
    private struct TransitionState {
        public float t;           // 0..1 progress
        public float duration;    // seconds
        public Color from;
        public Color to;
        public bool active;       // whether this label is currently the active target
    }

    private List<TransitionState> _transitions = new List<TransitionState>();

    void Awake() {
        // Initialize transitions list to labels count
        _transitions.Clear();
        for (int i = 0; i < labels.Count; i++) {
            var ts = new TransitionState {
                t = 1f,
                duration = 0f,
                from = baseColor,
                to = baseColor,
                active = false
            };
            _transitions.Add(ts);
        }
    }

    void Start() {
        // Initialize all labels to base color
        ApplyInstant(-1);
        if (videoPlayer != null) {
            // When the video loops/seeks/starts, refresh once
            videoPlayer.loopPointReached += _ => ForceRefresh();
            videoPlayer.started += _ => ForceRefresh();
            videoPlayer.seekCompleted += _ => ForceRefresh();
        }
    }

    void Update() {
        float t = GetTimeSeconds();
        int active = GetActiveIndex(t);

        if (active != _lastActiveIndex) {
            StartTransitions(active);
            _lastActiveIndex = active;
        }

        // Advance transitions and apply colors each frame
        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < labels.Count; i++) {
            if (labels[i]?.text == null) continue;
            var st = _transitions[i];
            if (st.t < 1f && st.duration > 0f) {
                st.t = Mathf.Min(1f, st.t + dt / st.duration);
                _transitions[i] = st; // write-back
            }
            float k = ease.Evaluate(st.t);
            labels[i].text.color = Color.Lerp(st.from, st.to, k);
        }
    }

    float GetTimeSeconds() {
        if (useManualTime) return manualTime;
        if (videoPlayer != null && videoPlayer.frameCount > 0) return (float)videoPlayer.time;
        // Fallback: drive by unscaled time since start
        return Time.unscaledTime;
    }

    int GetActiveIndex(float t) {
        for (int i = 0; i < labels.Count; i++) {
            var L = labels[i];
            if (L == null || L.text == null) continue;
            if (t >= L.start && t < L.end) return i;
        }
        return highlightNoneWhenNoMatch ? -1 : _lastActiveIndex;
    }

    void StartTransitions(int activeIndex) {
        // Prepare transitions for all labels
        EnsureTransitionListSize();
        for (int i = 0; i < labels.Count; i++) {
            var txt = labels[i]?.text;
            if (txt == null) continue;

            bool isActivating = (i == activeIndex);
            var st = _transitions[i];

            // Start a new transition from current visual color to target
            st.from = txt.color;
            st.to = isActivating ? highlightColor : baseColor;
            st.t = 0f;
            st.duration = isActivating ? Mathf.Max(0.0001f, fadeInDuration) : Mathf.Max(0.0001f, fadeOutDuration);
            st.active = isActivating;
            _transitions[i] = st;
        }
    }

    void ApplyInstant(int activeIndex) {
        EnsureTransitionListSize();
        for (int i = 0; i < labels.Count; i++) {
            var txt = labels[i]?.text;
            if (txt == null) continue;
            Color target = (i == activeIndex) ? highlightColor : baseColor;
            txt.color = target;
            _transitions[i] = new TransitionState { t = 1f, duration = 0f, from = target, to = target, active = (i == activeIndex) };
        }
    }

    void EnsureTransitionListSize() {
        while (_transitions.Count < labels.Count) {
            _transitions.Add(new TransitionState { t = 1f, duration = 0f, from = baseColor, to = baseColor, active = false });
        }
    }

    public void ForceRefresh() {
        _lastActiveIndex = -999; // force next Update to repaint
    }

    // Optional: external driver can call this with the current cue time
    public void SetManualTime(float seconds) {
        manualTime = seconds;
        useManualTime = true;
        ForceRefresh();
    }
}
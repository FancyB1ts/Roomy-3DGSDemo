using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPreviewSwap : MonoBehaviour
{
    [Header("Objects")]
    public GameObject placeholderGO;   // Your VideoPreview image (RawImage on this GO)
    public GameObject videoGO;         // The GO that shows the video (RawImage with RT). Can be the same GO that holds VideoPlayer.

    [Header("Video")]
    public VideoPlayer videoPlayer;    // The VideoPlayer (outputs to a RenderTexture)
    public bool prepareOnEnable = true;
    public bool autoPlayOnReady = true;

    [Header("Optional Fade")]
    public bool fadeIn = true;
    public float fadeDuration = 0.15f;

    RawImage _videoImg;
    Color _videoStartColor = Color.white;
    bool _revealedFirstFrame = false;

    bool HasSource(VideoPlayer vp)
    {
        return (vp && (vp.clip != null || !string.IsNullOrEmpty(vp.url)));
    }

    void Awake()
    {
        // Auto-find components on the same object if not assigned
        if (!videoPlayer) videoPlayer = GetComponentInChildren<VideoPlayer>(true);
        if (!videoGO) videoGO = gameObject; // assume this script sits on the video GO
        _videoImg = videoGO ? videoGO.GetComponent<RawImage>() : null;
        if (_videoImg) _videoStartColor = _videoImg.color;

        if (videoPlayer)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.sendFrameReadyEvents = true; // fire when first real frame is available
            videoPlayer.frameReady += OnFrameReady;
        }
    }

    void OnEnable()
    {
        // Show placeholder; keep the video GO active so VideoPlayer can Prepare.
        if (placeholderGO) placeholderGO.SetActive(true);

        // Hide only the visual by alpha (keep component enabled so RT binding is valid)
        if (_videoImg)
        {
            _videoImg.enabled = true;  // ensure component is alive
            if (fadeIn) SetAlpha(_videoImg, 0f); // start transparent
        }

        Debug.Log($"[VideoPreviewSwap] OnEnable | hasSource={HasSource(videoPlayer)} | isPrepared={(videoPlayer ? videoPlayer.isPrepared : false)}");

        if (prepareOnEnable && videoPlayer)
        {
            if (HasSource(videoPlayer))
            {
                Debug.Log("[VideoPreviewSwap] Calling Prepare()");
                videoPlayer.Prepare();
                StartCoroutine(EnsurePreparedAndPlaying()); // safety fallback
            }
            else if (videoPlayer.isPrepared)
            {
                Debug.Log("[VideoPreviewSwap] Already prepared, swapping now");
                OnPrepared(videoPlayer);
            }
            else
            {
                // No source yet—likely assigned by another script this frame. Wait briefly, then prepare.
                StartCoroutine(WaitForSourceThenPrepare());
            }
        }
    }

    void OnDisable()
    {
        if (videoPlayer) videoPlayer.Stop();
        // Reset for next open
        if (_videoImg && fadeIn) SetAlpha(_videoImg, _videoStartColor.a);
    }

    public void PrepareNow()
    {
        Debug.Log("[VideoPreviewSwap] PrepareNow()");
        if (placeholderGO) placeholderGO.SetActive(true);
        if (_videoImg)
        {
            _videoImg.enabled = true;
            if (fadeIn) SetAlpha(_videoImg, 0f);
        }
        if (videoPlayer && HasSource(videoPlayer))
        {
            videoPlayer.Prepare();
            StartCoroutine(EnsurePreparedAndPlaying());
        }
        else
        {
            Debug.LogWarning("[VideoPreviewSwap] PrepareNow() called without a valid source (clip/url)");
        }
    }

    void OnPrepared(VideoPlayer vp)
    {
        Debug.Log("[VideoPreviewSwap] prepareCompleted received");

        _revealedFirstFrame = false; // reset per session

        // Ensure video starts if desired
        if (autoPlayOnReady && !vp.isPlaying)
            vp.Play();

        // Keep placeholder visible to mask the black RT clear until the very first frame
        // Start a watchdog in case frameReady is not supported
        StartCoroutine(WaitForFirstFrameThenSwap(vp, 1.0f));
    }

    IEnumerator FadeAlpha(RawImage img, float from, float to, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            SetAlpha(img, Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetAlpha(img, to);
    }

    IEnumerator EnsurePreparedAndPlaying(float timeout = 2f)
    {
        float t = 0f;
        while (videoPlayer && !videoPlayer.isPrepared && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer) yield break;

        if (videoPlayer.isPrepared)
        {
            Debug.Log("[VideoPreviewSwap] Fallback swap after wait");
            OnPrepared(videoPlayer);
        }
        else
        {
            Debug.LogWarning("[VideoPreviewSwap] Prepare timeout—no swap");
        }
    }

    IEnumerator WaitForSourceThenPrepare(float timeout = 2f)
    {
        float t = 0f;
        // wait until another script assigns clip/url
        while (videoPlayer && !HasSource(videoPlayer) && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null; // next frame
        }

        if (!videoPlayer) yield break;

        if (HasSource(videoPlayer))
        {
            Debug.Log("[VideoPreviewSwap] Source assigned late—preparing now");
            videoPlayer.Prepare();
            StartCoroutine(EnsurePreparedAndPlaying());
        }
        else
        {
            Debug.LogWarning("[VideoPreviewSwap] No clip/url assigned after wait—skipping prepare");
        }
    }

    void OnFrameReady(VideoPlayer vp, long frameIdx)
    {
        if (_revealedFirstFrame) return;
        Debug.Log($"[VideoPreviewSwap] First frame callback (frame {frameIdx})");
        RevealVideoNow();
    }

    IEnumerator WaitForFirstFrameThenSwap(VideoPlayer vp, float timeout)
    {
        float t = 0f;
        // wait until we detect any progress in playback
        while (!_revealedFirstFrame && vp && t < timeout)
        {
            if (vp.isPlaying && (vp.frame > 0 || vp.time > 0f))
            {
                Debug.Log("[VideoPreviewSwap] First frame detected by polling");
                RevealVideoNow();
                yield break;
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_revealedFirstFrame)
        {
            Debug.LogWarning("[VideoPreviewSwap] First frame not detected within timeout—revealing anyway");
            RevealVideoNow();
        }
    }

    void RevealVideoNow()
    {
        if (_revealedFirstFrame) return;
        _revealedFirstFrame = true;

        if (placeholderGO) placeholderGO.SetActive(false);

        if (_videoImg)
        {
            if (fadeIn && fadeDuration > 0f)
                StartCoroutine(FadeAlpha(_videoImg, 0f, _videoStartColor.a, fadeDuration));
            else
                SetAlpha(_videoImg, _videoStartColor.a);
        }
    }

    static void SetAlpha(RawImage img, float a)
    {
        var c = img.color;
        c.a = a;
        img.color = c;
    }
    
    void OnDestroy()
    {
        if (videoPlayer)
        {
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.frameReady -= OnFrameReady;
        }
    }
}
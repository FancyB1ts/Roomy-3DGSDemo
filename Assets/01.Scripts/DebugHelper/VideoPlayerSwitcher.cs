using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerSwitcher : MonoBehaviour
{
    public VideoPlayer player;

    [Header("Editor Debug")]
    public VideoClip editorDebugClip;

    [Header("Build Settings")]
    public string relativePath = "onboarding/01_PlaceFurniture.mp4";

    void Awake()
    {
#if UNITY_EDITOR
        if (player != null && editorDebugClip != null)
        {
            player.source = VideoSource.VideoClip;
            player.clip = editorDebugClip;
        }
#else
        if (player != null)
        {
            player.source = VideoSource.Url;
            player.url = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath);
        }
#endif
    }
}
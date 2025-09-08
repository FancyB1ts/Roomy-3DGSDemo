using UnityEngine;

public class Viewport_ResetRotateZoom : MonoBehaviour
{
    public Transform target;             // Model to reset
    public Camera orthoCamera;           // Your isometric RenderTexture camera

    [Header("Reset Settings")]
    public Vector3 defaultRotation = Vector3.zero;
    public Vector3 defaultPosition = Vector3.zero;
    public Vector3 defaultScale = Vector3.one;
    public float defaultZoomPercent = 100f;  // Resets to 100% zoom

    public float duration = 0.4f;

    private Coroutine resetRoutine;
    private float baseOrthoSize;

    void Start()
    {
        if (orthoCamera != null)
            baseOrthoSize = orthoCamera.orthographicSize * (100f / defaultZoomPercent); // recover base
    }

    public void ResetAll()
    {
        if (resetRoutine != null) StopCoroutine(resetRoutine);
        resetRoutine = StartCoroutine(SmoothReset());
    }

    private System.Collections.IEnumerator SmoothReset()
    {
        float time = 0f;

        Quaternion startRot = target.rotation;
        Quaternion endRot = Quaternion.Euler(defaultRotation);

        Vector3 startPos = target.localPosition;
        Vector3 startScale = target.localScale;

        float zoomStart = orthoCamera.orthographicSize;
        float zoomTarget = baseOrthoSize * (defaultZoomPercent / 100f);

        while (time < duration)
        {
            float t = time / duration;
            target.rotation = Quaternion.Slerp(startRot, endRot, t);
            target.localPosition = Vector3.Lerp(startPos, defaultPosition, t);
            target.localScale = Vector3.Lerp(startScale, defaultScale, t);

            if (orthoCamera != null)
                orthoCamera.orthographicSize = Mathf.Lerp(zoomStart, zoomTarget, t);

            time += Time.deltaTime;
            yield return null;
        }

        // Final snap
        target.rotation = endRot;
        target.localPosition = defaultPosition;
        target.localScale = defaultScale;

        if (orthoCamera != null)
            orthoCamera.orthographicSize = zoomTarget;
    }
}

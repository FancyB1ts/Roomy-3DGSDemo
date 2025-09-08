using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MaterialSTTransfer mirrors a source material's texture and ST (tiling/offset)
/// to a UI RawImage and/or another Renderer material. Useful for keeping a UI preview
/// pixel-identical to a 3D plane that already performs ST-based fitting.
/// 
/// Attach to any GameObject, assign references, and call TransferNow().
/// Optionally enable autoTransferEveryFrame while debugging.
/// </summary>
public class MaterialSTTransfer : MonoBehaviour
{
    [Header("Source (Material owner)")]
    [Tooltip("Renderer that holds the source material instance (e.g., GroundPlane_FloorplanReceiver)")]
    public Renderer sourceRenderer;

    [Header("Targets (choose one or both)")]
    [Tooltip("UI RawImage that should visually match the source material")]
    public RawImage targetRawImage;
    [Tooltip("Optional: another Renderer that should receive the same texture + ST")]
    public Renderer targetRenderer;

    [Header("Options")]
    [Tooltip("Copy _BaseColor if both materials support it (controls letterbox/bar color)")]
    public bool copyBaseColor = true;
    [Tooltip("Force the target(s) to use the same shader as the source")]
    public bool forceSameShaderOnTargets = true;
    [Tooltip("Copy the texture reference from source material to targets")]
    public bool copyTexture = true;
    [Tooltip("Debugging helper: mirror every LateUpdate()")] 
    public bool autoTransferEveryFrame = false;

    /// <summary>
    /// Perform the transfer once.
    /// </summary>
    [ContextMenu("Transfer Now")] 
    public void TransferNow()
    {
        var srcMat = SafeGetSourceMaterial();
        if (srcMat == null) return;

        // Read once
        var tex    = srcMat.mainTexture;
        var scale  = srcMat.mainTextureScale;
        var offset = srcMat.mainTextureOffset;
        var hasBase = copyBaseColor && srcMat.HasProperty("_BaseColor");
        var baseCol = hasBase ? srcMat.GetColor("_BaseColor") : Color.white;

        // → RawImage target
        if (targetRawImage != null)
        {
            EnsureRawImageMaterial(targetRawImage, srcMat);

            if (copyTexture) targetRawImage.texture = tex as Texture2D;
            targetRawImage.uvRect = new Rect(0, 0, 1, 1);

            if (forceSameShaderOnTargets && targetRawImage.material.shader != srcMat.shader)
                targetRawImage.material.shader = srcMat.shader;

            if (copyTexture) targetRawImage.material.mainTexture = tex;
            targetRawImage.material.mainTextureScale  = scale;
            targetRawImage.material.mainTextureOffset = offset;

            if (hasBase && targetRawImage.material.HasProperty("_BaseColor"))
                targetRawImage.material.SetColor("_BaseColor", baseCol);

            Canvas.ForceUpdateCanvases();
            targetRawImage.SetMaterialDirty();
            targetRawImage.SetVerticesDirty();
        }

        // → Renderer target
        if (targetRenderer != null)
        {
            var dstMat = targetRenderer.material; // ensures instance
            if (forceSameShaderOnTargets && dstMat.shader != srcMat.shader)
                dstMat.shader = srcMat.shader;

            if (copyTexture) dstMat.mainTexture = tex;
            dstMat.mainTextureScale  = scale;
            dstMat.mainTextureOffset = offset;

            if (hasBase && dstMat.HasProperty("_BaseColor"))
                dstMat.SetColor("_BaseColor", baseCol);

            targetRenderer.material = dstMat; // assign back explicitly
        }

        Debug.Log($"[MaterialSTTransfer] Copied ST → scale={scale}, offset={offset}, tex={(tex?tex.name:"null")} ");
    }

    private void LateUpdate()
    {
        if (autoTransferEveryFrame) TransferNow();
    }

    private Material SafeGetSourceMaterial()
    {
        if (sourceRenderer == null)
        {
            Debug.LogWarning("[MaterialSTTransfer] No sourceRenderer assigned");
            return null;
        }
        var mat = sourceRenderer.material; // instance
        if (mat == null)
        {
            Debug.LogWarning("[MaterialSTTransfer] Source renderer has no material instance");
            return null;
        }
        return mat;
    }

    private static void EnsureRawImageMaterial(RawImage img, Material src)
    {
        if (img.material == null)
        {
            img.material = new Material(src.shader);
            return;
        }
        // If UI material exists but uses a different shader and we want parity, swap
        // (We keep the same instance so we don't break references.)
        if (img.material.shader != src.shader)
        {
            var old = img.material;
            img.material = new Material(src.shader);
            if (old != null) Destroy(old);
        }
    }
}

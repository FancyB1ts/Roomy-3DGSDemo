using UnityEngine;

[ExecuteAlways]  // Also works in editor for live feedback
[RequireComponent(typeof(RectTransform))]
public class ClampStretchWidth : MonoBehaviour
{
    public float minWidth = 100f;
    public float maxWidth = 400f;

    void Update()
    {
        if (!isActiveAndEnabled) return;

        RectTransform rt = (RectTransform)transform;
        RectTransform parent = rt.parent as RectTransform;

        if (parent == null) return;

        float parentWidth = parent.rect.width;
        float clampedWidth = Mathf.Clamp(parentWidth, minWidth, maxWidth);

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, clampedWidth);
    }
}
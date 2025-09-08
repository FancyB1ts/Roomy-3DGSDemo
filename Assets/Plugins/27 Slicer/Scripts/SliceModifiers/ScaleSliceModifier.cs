// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// Modifies the scale of the <c>GameObject</c>, as if its scale vector is being sliced.
    /// </summary>
    /// <remarks>
    /// Currently this is a very simple calculation. It does not take into consideration if parts of the object being scaled lays outside of the sliced bounds.
    /// 
    /// This simple method should be correct or close enough most of the time.
    /// </remarks>
    /// <example>
    /// <br /><br />[REFERENCE MANUAL](xref:manual\components\scale_slice_modifier)
    /// </example>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Slicer/Scale Slice Modifier")]
    [HelpURL(SlicerConfiguration.SiteUrl + SlicerConfiguration.ComponentsManualPath + "scale_slice_modifier.html")]
    public class ScaleSliceModifier : SliceModifier
    {
        [HideInInspector]
        [SerializeField]
        private Vector3 originalScale = Vector3.one;

        /// <summary>
        /// The original scale of the Game Object
        /// </summary>
        public Vector3 OriginalScale { get { return originalScale; } }

        [HideInInspector]
        [SerializeField]
        private Vector3 slicedScale;

        /// <summary>
        /// The sliced scale of the Game Object
        /// </summary>
        public Vector3 SlicedScale { get { return slicedScale; } }

        /// <inheritdoc/>
        public override Hash128 GatherDetails()
        {
            var hash = base.GatherDetails();

            if (!ModifierEnabled)
            {
                if (ShouldUpdate())
                {
                    originalScale = transform.localScale;
                }
            }

            var positionHash = HashUtility.CalculateHash(originalScale);
            HashUtility.AppendHash(positionHash, ref hash);

            return hash;
        }

        /// <inheritdoc/>
        public override void Modify(Vector3 size, Transform rootTransform, Bounds completeBounds, Bounds slicedBounds)
        {
            if (ModifierEnabled)
            {
                slicedScale = Vector3.Scale(originalScale, size);
                transform.localScale = slicedScale;
            }
        }

        /// <inheritdoc/>
        public override void DisableModifier()
        {
            base.DisableModifier();

            if (transform != null)
            {
                transform.localScale = originalScale;
            }
        }

        /// <inheritdoc/>
        public override void EnableModifier()
        {
            if (transform != null)
            {
                transform.localScale = slicedScale;
            }

            base.EnableModifier();
        }

        /// <inheritdoc/>
        public override void FinalizeSlicing()
        {
            EnableModifier();

            originalScale = slicedScale;

            SlicerController.SafeDestroy(this);
        }

        private void OnDestroy()
        {
            DisableModifier();
        }

        public void SetUnslicedScale(Vector3 scale)
        {
            originalScale = scale;
            if (!ModifierEnabled)
            {
                transform.localScale = scale;
            }
        }
    }
}
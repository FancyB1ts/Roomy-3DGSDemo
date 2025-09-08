// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// Modifies the size property of the <c>SlicerController</c>, as if it is being sliced.
    /// </summary>
    /// <example>
    /// <br /><br />[REFERENCE MANUAL](xref:manual\components\slicer_controller_slice_modifier)
    /// </example>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Slicer/Slicer Controller Slice Modifier")]
    [RequireComponent(typeof(SlicerController))]
    [HelpURL(SlicerConfiguration.SiteUrl + SlicerConfiguration.ComponentsManualPath + "slicer_controller_slice_modifier.html")]
    public class SlicerControllerSliceModifier : SliceModifier
    {
        [HideInInspector]
        [SerializeField]
        private Vector3 originalSize = Vector3.one;

        /// <summary>
        /// The original size of the <see cref="SlicerController"/>
        /// </summary>
        public Vector3 OriginalSize { get { return originalSize; } }

        [HideInInspector]
        [SerializeField]
        private Vector3 originalOffset = Vector3.zero;

        /// <summary>
        /// The original offset of the <see cref="SlicerController"/>
        /// </summary>
        public Vector3 OriginalOffset { get { return originalOffset; } }

        [HideInInspector]
        [SerializeField]
        private Vector3 slicedSize;

        /// <summary>
        /// The sliced size of the <see cref="SlicerController"/>
        /// </summary>
        public Vector3 SlicedSize { get { return slicedSize; } }

        [HideInInspector]
        [SerializeField]
        private Vector3 slicedOffset;

        /// <summary>
        /// The sliced offset of the <see cref="SlicerController"/>
        /// </summary>
        public Vector3 SlicedOffset { get { return slicedOffset; } }

        private SlicerController slicerController;

        private void Awake()
        {
            slicerController = GetComponent<SlicerController>();
        }

        /// <inheritdoc/>
        public override Hash128 GatherDetails()
        {
            var hash = base.GatherDetails();

            if (slicerController != null && !ModifierEnabled)
            {
                if (ShouldUpdate())
                {
                    originalSize = slicerController.Size;
                    originalOffset = slicerController.Offset;
                }
            }

            var sizeHash = HashUtility.CalculateHash(originalSize);
            var offsetHash = HashUtility.CalculateHash(originalOffset);
            HashUtility.AppendHash(sizeHash, offsetHash, ref hash);

            return hash;
        }

        /// <inheritdoc/>
        public override void Modify(Vector3 size, Transform rootTransform, Bounds completeBounds, Bounds slicedBounds)
        {
            if (!ModifierEnabled || slicerController == null)
            {
                return;
            }

            if (!slicerController.CompleteBounds.HasValue)
            {
                slicerController.RefreshSliceImmediate();
            }

            var bounds = slicerController.CompleteBounds.Value;
            var slicedOriginalBounds = SliceUtility.SliceVerts(bounds, transform, size, rootTransform, completeBounds, slicedBounds);

            slicedSize = new Vector3(slicedOriginalBounds.size.x / bounds.size.x, slicedOriginalBounds.size.y / bounds.size.y, slicedOriginalBounds.size.z / bounds.size.z);

            var matrix = MatrixUtility.BuildTransformMatrix(rootTransform, transform);
            var originalCenterLocal = matrix.MultiplyPoint3x4(bounds.center);
            var slicedOriginalCenterLocal = matrix.MultiplyPoint3x4(slicedOriginalBounds.center);

            slicedOffset = slicedOriginalCenterLocal - originalCenterLocal;
            slicerController.Size = slicedSize;
            slicerController.Offset = slicedOffset;
            slicerController.RefreshSlice();
        }

        /// <inheritdoc/>
        public override void DisableModifier()
        {
            base.DisableModifier();

            if (slicerController != null)
            {
                slicerController.Size = originalSize;
                slicerController.Offset = originalOffset;
            }
        }

        /// <inheritdoc/>
        public override void EnableModifier()
        {
            if (slicerController != null)
            {
                slicerController.Size = slicedSize;
                slicerController.Offset = slicedOffset;
            }

            base.EnableModifier();
        }

        /// <inheritdoc/>
        public override void FinalizeSlicing()
        {
            EnableModifier();

            originalSize = slicedSize;
            originalOffset = slicedOffset;

            SlicerController.SafeDestroy(this);
        }

        private void OnDestroy()
        {
            DisableModifier();
        }

        /// <summary>
        /// Sets the size of the Slicer Controller before slicing.
        /// </summary>
        /// <remarks>
        /// Call <see cref="SlicerController.RefreshSlice"/> for this change to be sliced.
        /// </remarks>
        /// <param name="size">The new unsliced size to set</param>
        public void SetUnslicedSize(Vector3 size)
        {
            originalSize = size;
            if (!ModifierEnabled && slicerController != null)
            {
                slicerController.Size = originalSize;
                slicerController.Offset = originalOffset;
            }
        }
    }
}
// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEngine;
using Slicer.Core;
using System;

namespace Slicer
{
    /// <summary>
    /// Modifies the position of the <c>GameObject</c>, as if its position vector is being sliced.
    /// </summary>
    /// <example>
    /// <br /><br />[REFERENCE MANUAL](xref:manual\components\position_slice_modifier)
    /// </example>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Slicer/Position Slice Modifier")]
    [HelpURL(SlicerConfiguration.SiteUrl + SlicerConfiguration.ComponentsManualPath + "position_slice_modifier.html")]
    public class PositionSliceModifier : SliceModifier
    {
        /// <summary>
        /// The anchor (offset) position to be used by this modifier.
        /// </summary>
        [Tooltip("The anchor (offset) position to be used by this modifier.")]
        public Vector3 Anchor;

        [HideInInspector]
        [SerializeField]
        private Vector3 originalPosition = Vector3.zero;

        /// <summary>
        /// The original position of the Game Object
        /// </summary>
        public Vector3 OriginalPosition { get { return originalPosition; } }

        [HideInInspector]
        [SerializeField]
        private Vector3 slicedPosition;

        /// <summary>
        /// The sliced position of the Game Object
        /// </summary>
        public Vector3 SlicedPosition { get { return slicedPosition; } }

        /// <inheritdoc/>
        public override Hash128 GatherDetails()
        {
            var hash = base.GatherDetails();

            var anchorHash = HashUtility.CalculateHash(Anchor);
            HashUtility.AppendHash(anchorHash, ref hash);

            if (!ModifierEnabled)
            {
                if (ShouldUpdate())
                {
                    originalPosition = transform.localPosition;
                }
            }

            var positionHash = HashUtility.CalculateHash(originalPosition);
            HashUtility.AppendHash(positionHash, ref hash);

            return hash;
        }

        /// <inheritdoc/>
        public override void Modify(Vector3 size, Transform rootTransform, Bounds completeBounds, Bounds slicedBounds)
        {
            if (ModifierEnabled)
            {
                var anchor = transform.localRotation * Anchor;
                var newPos = SliceUtility.SliceVector(originalPosition + anchor, size, completeBounds, slicedBounds);
                slicedPosition = newPos - anchor;
                transform.localPosition = slicedPosition;
            }
        }

        /// <inheritdoc/>
        public override void DisableModifier()
        {
            base.DisableModifier();

            if (transform != null)
            {
                transform.localPosition = originalPosition;
            }
        }

        /// <inheritdoc/>
        public override void EnableModifier()
        {
            if (transform != null)
            {
                transform.localPosition = slicedPosition;
            }

            base.EnableModifier();
        }

        /// <inheritdoc/>
        public override void FinalizeSlicing()
        {
            EnableModifier();

            originalPosition = slicedPosition;

            SlicerController.SafeDestroy(this);
        }

        private void OnDestroy()
        {
            DisableModifier();
        }

        /// <summary>
        /// Sets the position of the Game Object before slicing.
        /// </summary>
        /// <remarks>
        /// Call <see cref="SlicerController.RefreshSlice"/> for this change to be sliced.
        /// </remarks>
        /// <param name="position">The new unsliced position to set</param>
        public void SetUnslicedPosition(Vector3 position)
        {
            originalPosition = position;
            if (!ModifierEnabled)
            {
                transform.localPosition = originalPosition;
            }
        }
    }
}
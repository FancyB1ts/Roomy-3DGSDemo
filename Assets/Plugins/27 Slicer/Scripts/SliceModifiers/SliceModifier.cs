// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using System;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// The base class for Slice Modifiers.
    /// </summary>
    /// <example>
    /// <br /><br />[REFERENCE MANUAL](xref:manual\components\slice_modifiers)
    /// </example>
    public abstract class SliceModifier : MonoBehaviour
    {
        /// <summary>
        /// Is this modifier currently enabled.
        /// </summary>
        /// <value><c>true</c> by default.</value>
        [HideInInspector]
        [NonSerialized]
        public bool ModifierEnabled = true;

        /// <summary>
        /// When set to true the properties for this modifier will no longer be updated when Unity is playing and its parent <c>SlicerController</c> is in edit mode.
        /// </summary>
        /// <remarks>
        /// This is useful to have turned on when you want the physics simulation to take over control of this modifier.
        /// </remarks>
        [Tooltip("Only allow updating when Unity is not playing.")]
        public bool DoNotUpdateInPlayMode = true;

        /// <summary>
        /// Gathers details required for applying this modification.
        /// </summary>
        public virtual Hash128 GatherDetails()
        {
            var enabledHash = HashUtility.CalculateHash(ModifierEnabled, 1);

            return enabledHash;
        }

        /// <summary>
        /// Applies the modification to the tracked items.
        /// </summary>
        /// <param name="size">The final size (as a scale) of all the items that are to be sliced. See <see cref="SlicerController.Size"/> for more information.</param>
        /// <param name="rootTransform">The transform of the parent <see cref="SlicerController"/></param>
        /// <param name="completeBounds">The complete bounding box of all sliced items after being sliced. See <see cref="SlicerController.CompleteBounds"/> for more information.</param>
        /// <param name="slicedBounds">The bounding box of the slices that will be made. See <see cref="SlicerController.SlicedBounds"/> for more information.</param>
        public abstract void Modify(Vector3 size, Transform rootTransform, Bounds completeBounds, Bounds slicedBounds);

        /// <summary>
        /// Enables this Slice Modifier.
        /// </summary>
        public virtual void EnableModifier()
        {
            ModifierEnabled = true;
        }

        /// <summary>
        /// Disables this Slice Modifier.
        /// </summary>
        public virtual void DisableModifier()
        {
            ModifierEnabled = false;
        }

        /// <summary>
        /// Finalizes slicing for this Slice Modifier.
        /// </summary>
        public abstract void FinalizeSlicing();

        /// <summary>
        /// Returns true when this SliceModifier is able to update its modification.
        /// </summary>
        protected bool ShouldUpdate()
        {
            return !DoNotUpdateInPlayMode || (DoNotUpdateInPlayMode && !Application.isPlaying);
        }

        /// <summary>
        /// Gets the <see cref="SlicerController"/> that is manging this modifier.
        /// </summary>
        /// <remarks>
        /// This iterates over it's ancestors Game Object. So storing the result may be more performant than repeatedly calling this function.
        /// </remarks>
        /// <returns>The parent slicer controller.</returns>
        public SlicerController GetParentSlicerController()
        {
            var slicerController = GetParentSlicerController(transform.parent);
            return slicerController;
        }

        private SlicerController GetParentSlicerController(Transform transform)
        {
            var hasComponent = transform.TryGetComponent<SlicerController>(out var slicerController);

            if (!hasComponent)
            {
                GetParentSlicerController(transform.parent);
            }

            return slicerController;
        }
    }
}
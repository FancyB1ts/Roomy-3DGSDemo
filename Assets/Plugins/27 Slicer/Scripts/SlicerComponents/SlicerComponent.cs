// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using System;
using UnityEngine;
using Slicer.Core;

namespace Slicer
{
    /// <summary>
    /// The base class for all of the Slicers
    /// </summary>
    /// <example>
    /// <br /><br />[REFERENCE MANUAL](xref:manual\components\slicer_components)
    /// </example>
    [RequireComponent(typeof(SlicerController))]
    [DefaultExecutionOrder(650)]
    public abstract class SlicerComponent : MonoBehaviour
    {
        /// <summary>
        /// Is the slicing currently enabled.
        /// </summary>
        /// <value><c>true</c> by default.</value>
        public bool SlicingEnabled { get; private set; } = true;

        /// <summary>
        /// When set to true it will not calculate the bounds for the items it is tracking.
        /// </summary>
        /// <value><c>false</c> by default.</value>
        [Tooltip("When this property is checked it will not calculate the bounds for the items it is tracking.")]
        public bool SkipBoundsCalculation = false;

        /// <summary>
        /// It is called by the parent <see cref="SlicerController"/> prior to calling <see cref="GatherDetails"/>.
        /// </summary>
        /// <remarks>
        /// It is primarily used to set the <see cref="Details.Remove"/> field to false.
        /// The <see cref="Details"/> that are still false after <see cref="GatherDetails"/> is run will be removed by <see cref="PostGatherDetails"/>.
        /// </remarks>
        public virtual void PreGatherDetails() { }

        /// <summary>
        /// Gathers details required for slicing.
        /// It is called by the parent <see cref="SlicerController"/> and used to search for valid items to slice and gather details on those items.
        /// </summary>
        /// <param name="childTransform">The transform of the GameObject that will be searched for suitable item/s to slice.</param>
        /// <param name="rootTransform">The transform of the GameObject containing the <see cref="SlicerController"/>.</param>
        public abstract void GatherDetails(Transform childTransform, Transform rootTransform);

        /// <summary>
        /// It is called by the parent <see cref="SlicerController"/> after calling <see cref="GatherDetails"/>.
        /// </summary>
        /// <remarks>
        /// It is primarily used to remove <see cref="Details"/> that have <see cref="Details.Remove"/> field set to false after <see cref="GatherDetails"/> is run.
        /// 
        /// It also calculated the hash of all the details that where gathered during the previous execution of <see cref="GatherDetails"/>.
        /// This hash is then used to determine if the details have changed enough to require re-slicing the items.
        /// </remarks>
        /// <returns>The hash of the details gathered during the previous execution of <see cref="GatherDetails"/>.</returns>
        public virtual Hash128 PostGatherDetails()
        {
            var enabledHash = HashUtility.CalculateHash(SlicingEnabled, 1);
            var skipBoundsCalculationHash = HashUtility.CalculateHash(SkipBoundsCalculation, 2);
            HashUtility.AppendHash(skipBoundsCalculationHash, ref enabledHash);

            return enabledHash;
        }

        /// <summary>
        /// Calculates the bounds for all of the items being managed by this SlicerComponent.
        /// 
        /// It is in Local Object Space the parent SlicerController.
        /// </summary>
        /// <remarks>
        /// These bounds are calculated from details gathered while running <see cref="GatherDetails"/>.
        /// </remarks>
        /// <returns>
        /// Returns the bounds in Local Object Space of the parent SlicerController.
        /// Returns null if there are no items that have valid bounds.
        /// </returns>
        public abstract Bounds? CalculateBounds();

        /// <summary>
        /// Slices the tracked items, this is where the magic happens!
        /// </summary>
        /// <param name="size">The final size (as a scale) of all the items that are to be sliced. See <see cref="SlicerController.Size"/> for more information.</param>
        /// <param name="rootTransform">The transform of the parent <see cref="SlicerController"/></param>
        /// <param name="completeBounds">The complete bounding box of all sliced items after being sliced. See <see cref="SlicerController.CompleteBounds"/> for more information.</param>
        /// <param name="slicedBounds">The bounding box of the slices that will be made in object space. See <see cref="SlicerController.SlicedBounds"/> for more information.</param>
        /// <param name="slices">The slices that have been chosen, Ranging from 0 (the center of the object), to 1 being the furthest extents of the object. See <see cref="SlicerController.Slices"/> for more information.</param>
        public abstract void Slice(Vector3 size, Transform rootTransform, Bounds completeBounds, Bounds slicedBounds, Vector3 slices);

        /// <summary>
        /// Enables slicing for this SlicerComponent.
        /// </summary>
        public virtual void EnableSlicing()
        {
            SlicingEnabled = true;
        }

        /// <summary>
        /// Disables slicing for this SlicerComponent.
        /// </summary>
        public virtual void DisableSlicing()
        {
            SlicingEnabled = false;
        }

        /// <summary>
        /// Finalizes slicing for this Slicer Component.
        /// </summary>
        public abstract void FinalizeSlicing();
    }
}
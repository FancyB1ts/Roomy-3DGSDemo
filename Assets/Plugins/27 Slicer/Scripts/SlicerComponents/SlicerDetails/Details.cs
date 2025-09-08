// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using System;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// The base class of details about an item that is being sliced.
    /// </summary>
    [Serializable]
    public abstract class Details
    {
        /// <summary>
        /// The Id of the item being tracked.
        /// </summary>
        /// <remarks>
        /// This ID is normally obtained using the <c>MonoBehaviour.GetInstanceID()</c> of the primary component used during slicing.
        /// </remarks>
        /// <example>
        /// For example the following Slicer Components use these IDs.
        ///  - <see cref="MeshSlicerComponent"/>: <c>MeshFilter.GetInstanceID()</c>
        ///  - <see cref="ColliderSlicerComponent"/>: <c>Collider.GetInstanceID()</c>
        /// </example>
        public int Id;

        /// <summary>
        /// The Transform on the GameObject being tracked by this details object.
        /// </summary>
        public Transform Transform;

        /// <summary>
        /// During <see cref="SlicerComponent.PreGatherDetails"/> this bool will be set to true.
        /// If it is used during <see cref="SlicerComponent.GatherDetails"/> is will be set to false.
        /// Any Details that remain false during <see cref="SlicerComponent.PostGatherDetails"/> will be removed from the list.
        /// </summary>
        [NonSerialized]
        public bool Remove;

        /// <summary>
        /// Used to destroy any components that is being managing.
        /// </summary>
        public abstract void Destroy();

        /// <summary>
        /// Resets any component to its original state.
        /// </summary>
        public abstract void DisableSlicing();

        /// <summary>
        /// Sets any component to its sliced state.
        /// </summary>
        public abstract void EnableSlicing();

        /// <summary>
        /// Readies this component up to finalize slicing
        /// </summary>
        public abstract void FinalizeSlicing();

        /// <summary>
        /// Calculates the hash of this details object using important properties.
        /// </summary>
        /// <returns>The calculated hash.</returns>
        public virtual Hash128 CalculateHash()
        {
            var hash = HashUtility.CalculateHash(Id);

            var posHash = HashUtility.CalculateHash(Transform.position);
            var rotHash = HashUtility.CalculateHash(Transform.rotation);
            var scaleHash = HashUtility.CalculateHash(Transform.lossyScale);
            HashUtility.AppendHash(posHash, rotHash, scaleHash, ref hash);

            return hash;
        }
    }
}

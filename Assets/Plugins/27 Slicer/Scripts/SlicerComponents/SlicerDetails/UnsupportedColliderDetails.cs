// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using System;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// BoxColliderDetails is used to keep track of information about a single Collider that does not have a simple way to slice normally.
    /// </summary>
    [Serializable]
    public class UnsupportedColliderDetails : ColliderDetails
    {
        /// <summary>
        /// The Collider on the GameObject being tracked by this details object.
        /// </summary>
        [SerializeReference]
        public Collider Collider;

        /// <inheritdoc/>
        public override void DisableSlicing()
        {
        }

        /// <inheritdoc/>
        public override void EnableSlicing()
        {
        }

        /// <inheritdoc/>
        public override void FinalizeSlicing()
        {
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
        }
    }
}

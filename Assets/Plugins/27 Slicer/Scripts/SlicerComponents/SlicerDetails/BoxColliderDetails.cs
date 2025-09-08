// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using System;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// BoxColliderDetails is used to keep track of information about a single BoxCollider that is being sliced.
    /// </summary>
    [Serializable]
    public class BoxColliderDetails : ColliderDetails
    {
        /// <summary>
        /// The BoxCollider on the GameObject being tracked by this details object.
        /// </summary>
        [SerializeReference]
        public BoxCollider BoxCollider;

        /// <summary>
        /// The BoxColliders original properties, measured in the object's local space.
        /// </summary>
        /// <remarks>
        /// This is the original values of <c>BoxCollider.center</c> and <c>BoxCollider.size</c> before the BoxCollider was sliced.
        /// </remarks>
        public Bounds OriginalColliderProperties;

        /// <summary>
        /// The BoxColliders sliced properties, measured in the object's local space.
        /// </summary>
        /// <remarks>
        /// This is the sliced values of <c>BoxCollider.center</c> and <c>BoxCollider.size</c> after the BoxCollider is sliced.
        /// </remarks>
        public Bounds SlicedColliderProperties;

        /// <inheritdoc/>
        public override void DisableSlicing()
        {
            if (BoxCollider != null)
            {
                BoxCollider.center = OriginalColliderProperties.center;
                BoxCollider.size = OriginalColliderProperties.size;
            }
        }

        /// <inheritdoc/>
        public override void EnableSlicing()
        {
            if (BoxCollider != null)
            {
                BoxCollider.center = SlicedColliderProperties.center;
                BoxCollider.size = SlicedColliderProperties.size;
            }
        }

        /// <inheritdoc/>
        public override void FinalizeSlicing()
        {
            EnableSlicing();
        }

        /// <inheritdoc/>
        public override Hash128 CalculateHash()
        {
            var hash = base.CalculateHash();

            var originalColliderPropertiesHash = HashUtility.CalculateHash(OriginalColliderProperties);
            HashUtility.AppendHash(originalColliderPropertiesHash, ref hash);

            if (BoxCollider != null)
            {
                var enabledColliderHash = HashUtility.CalculateHash(BoxCollider.enabled, 5);
                HashUtility.AppendHash(enabledColliderHash, ref hash);
            }

            return hash;
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            // Nothing to destroy.
        }
    }
}

// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using System;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// The base class for all collider details
    /// </summary>
    [Serializable]
    public abstract class ColliderDetails : Details
    {
        /// <summary>
        /// The Bounds of the collider before it is sliced, in local space of the parent SlicerComponent.
        /// </summary>
        public Bounds OriginalBounds;

        /// <inheritdoc/>
        public override Hash128 CalculateHash()
        {
            var hash = base.CalculateHash();

            var originalBoundsHash = HashUtility.CalculateHash(OriginalBounds);
            HashUtility.AppendHash(originalBoundsHash, ref hash);

            return hash;
        }
    }
}

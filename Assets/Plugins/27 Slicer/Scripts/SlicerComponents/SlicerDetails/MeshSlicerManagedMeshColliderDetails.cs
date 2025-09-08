// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using System;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// MeshSlicerManagedMeshColliderDetails is used to keep track of information about a single MeshCollider that uses a mesh that is already being sliced by a <see cref="MeshSlicerComponent"/>.
    /// </summary>
    [Serializable]
    public class MeshSlicerManagedMeshColliderDetails : ColliderDetails
    {
        /// <summary>
        /// The MeshCollider on the GameObject being tracked by this details object.
        /// </summary>
        [SerializeReference]
        public MeshCollider MeshCollider;

        /// <summary>
        /// The MeshDetails of the mesh that is already being sliced by a <see cref="MeshSlicerComponent"/>.
        /// </summary>
        [SerializeReference]
        public MeshDetails MeshSlicerDetails;

        /// <summary>
        /// The previous hash of the Verts after slicing.
        /// </summary>
        /// <remarks>
        /// This is used to determine if the <see cref="MeshSlicerComponent"/> has just updated the verts of the tracked Mesh.
        /// </remarks>
        [NonSerialized]
        public Hash128 LastVertHash;

        public void ResetHashes()
        {
            LastVertHash = HashUtility.CalculateHash(Time.frameCount * 7);
        }

        /// <inheritdoc/>
        public override void DisableSlicing()
        {
            if (MeshCollider != null && MeshSlicerDetails != null && MeshCollider.sharedMesh == MeshSlicerDetails.SlicedMesh)
            {
                MeshCollider.sharedMesh = MeshSlicerDetails.OriginalSharedMesh;
            }
        }

        /// <inheritdoc/>
        public override void EnableSlicing()
        {
            if (MeshCollider != null && MeshSlicerDetails != null && MeshSlicerDetails.SlicedMesh != null)
            {
                MeshCollider.sharedMesh = MeshSlicerDetails.SlicedMesh;
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

            if (MeshSlicerDetails != null)
            {
                var tempHash = MeshSlicerDetails.CalculateHash();
                HashUtility.AppendHash(tempHash, ref hash);
            }

            if (MeshCollider != null)
            {
                var enabledColliderHash = HashUtility.CalculateHash(MeshCollider.enabled, 5);
                HashUtility.AppendHash(enabledColliderHash, ref hash);

                if (MeshCollider.sharedMesh != null)
                {
                    var tempHash = HashUtility.CalculateHash(MeshCollider.sharedMesh);
                    HashUtility.AppendHash(tempHash, ref hash);
                }
            }

            return hash;
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            // Nothing to destroy, let the Mesh Slicer manage this
        }
    }
}

// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using System;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// MeshColliderDetails is used to keep track of information about a single MeshCollider that is being sliced.
    /// </summary>
    [Serializable]
    public class MeshColliderDetails : ColliderDetails
    {
        /// <summary>
        /// The MeshCollider on the GameObject being tracked by this details object.
        /// </summary>
        [SerializeReference]
        public MeshCollider MeshCollider;

        /// <summary>
        /// The Original Mesh on the GameObject being tracked by this details object.
        /// </summary>
        /// <remarks>
        /// This is the original mesh that is used as the basis for slicing when <see cref="SlicerController.Slice"/> is called.
        /// </remarks>
        [SerializeReference]
        public Mesh OriginalSharedMesh;
        /// <summary>
        /// The Modified Mesh on the GameObject being tracked by this details object.
        /// </summary>
        /// <remarks>
        /// This is the mesh that is sliced when <see cref="SlicerController.Slice"/> is called.
        /// </remarks>
        [SerializeReference]
        public Mesh SlicedMesh;

        /// <summary>
        /// The Original Mesh on the GameObject being tracked by this details object.
        /// </summary>
        /// <remarks>
        /// This is the original mesh that is used as the basis for slicing when <see cref="SlicerController.Slice"/> is called.
        /// </remarks>
        [SerializeField]
        public Hash128 SlicedVertHash;

        /// <inheritdoc/>
        public override void Destroy()
        {
            DestroySlicedMesh();
        }

        /// <summary>
        /// Destroys the Sliced Mesh
        /// </summary>
        public void DestroySlicedMesh()
        {
            if (SlicedMesh == null)
            {
                return;
            }

            SlicerController.SafeDestroy(SlicedMesh);
        }

        /// <summary>
        /// Resets the mesh hashes
        /// </summary>
        public void ResetHashes()
        {
            SlicedVertHash = HashUtility.CalculateHash(Time.frameCount * 3);
        }

        /// <inheritdoc/>
        public override void DisableSlicing()
        {
            if (MeshCollider != null && MeshCollider.sharedMesh == SlicedMesh)
            {
                MeshCollider.sharedMesh = OriginalSharedMesh;
            }
        }

        /// <inheritdoc/>
        public override void EnableSlicing()
        {
            if (MeshCollider != null && SlicedMesh != null)
            {
                MeshCollider.sharedMesh = SlicedMesh;
            }
        }

        /// <inheritdoc/>
        public override void FinalizeSlicing()
        {
            EnableSlicing();

            if (Application.isPlaying && SlicerConfiguration.UseFinalizedSlicedMeshComponent)
            {
                var finalizedSlicedMesh = MeshCollider.gameObject.AddComponent<FinalizedSlicedMesh>();
                finalizedSlicedMesh.SlicedMesh = SlicedMesh;
            }

            if (SlicerConfiguration.OptimizeFinalizedSlicedMesh)
            {
                SlicedMesh.Optimize();
            }

            if (SlicerConfiguration.SetFinalizedSlicedMeshToBeNoLongerReadable)
            {
                SlicedMesh.UploadMeshData(true);
            }
        }

        /// <inheritdoc/>
        public override Hash128 CalculateHash()
        {
            var hash = base.CalculateHash();

            if (OriginalSharedMesh != null)
            {
                var tempHash = HashUtility.CalculateHash(OriginalSharedMesh);
                HashUtility.AppendHash(tempHash, ref hash);
            }

            if (SlicedMesh != null)
            {
                var tempHash = HashUtility.CalculateHash(SlicedMesh);
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
    }
}

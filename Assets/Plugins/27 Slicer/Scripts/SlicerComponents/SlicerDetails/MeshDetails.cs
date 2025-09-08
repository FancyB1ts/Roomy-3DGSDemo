// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using System;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// MeshDetails is used to keep track of information about a single Mesh that is being sliced.
    /// </summary>
    [Serializable]
    public class MeshDetails : Details
    {
        /// <summary>
        /// The MeshFilter on the GameObject being tracked by this details object.
        /// </summary>
        public MeshFilter MeshFilter;

        /// <summary>
        /// The Original Mesh on the GameObject being tracked by this details object.
        /// </summary>
        /// <remarks>
        /// This is the original mesh that is used as the basis for slicing when <see cref="SlicerController.Slice"/> is called.
        /// </remarks>
        public Mesh OriginalSharedMesh;
        /// <summary>
        /// The Modified Mesh on the GameObject being tracked by this details object.
        /// </summary>
        /// <remarks>
        /// This is the mesh that is sliced when <see cref="SlicerController.Slice"/> is called.
        /// </remarks>
        public Mesh SlicedMesh;

        /// <summary>
        /// The MeshRenderer on the GameObject being tracked by this details object.
        /// </summary>
        public MeshRenderer MeshRenderer;

        /// <summary>
        /// The Bounds of the mesh before it is sliced, in local space of the parent SlicerComponent.
        /// </summary>
        public Bounds? OriginalBounds;

        /// <summary>
        /// The hash of the Verts after slicing.
        /// </summary>
        /// <remarks>
        /// This is set to the value of <see cref="SliceUtility.SkipVertHash"/> when Vert Slicing is being skipped.
        /// </remarks>
        [SerializeField]
        public Hash128 SlicedVertHash;

        /// <summary>
        /// The hash of the UVs after slicing.
        /// </summary>
        /// <remarks>
        /// This is set to the value of <see cref="SliceUtility.SkipUvHash"/> when UV Slicing is being skipped.
        /// </remarks>
        [SerializeField]
        public Hash128 SlicedUvHash;

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

        /// <inheritdoc/>
        public override void DisableSlicing()
        {
            if (MeshFilter != null && MeshFilter.sharedMesh == SlicedMesh)
            {
                MeshFilter.sharedMesh = OriginalSharedMesh;
            }
        }

        /// <inheritdoc/>
        public override void EnableSlicing()
        {
            if (MeshFilter != null && SlicedMesh != null)
            {
                MeshFilter.sharedMesh = SlicedMesh;
            }
        }

        /// <inheritdoc/>
        public override void FinalizeSlicing()
        {
            EnableSlicing();

            if (Application.isPlaying && SlicerConfiguration.UseFinalizedSlicedMeshComponent)
            {
                var finalizedSlicedMesh = MeshFilter.gameObject.AddComponent<FinalizedSlicedMesh>();
                finalizedSlicedMesh.SlicedMesh = SlicedMesh;
            }

            if (SlicerConfiguration.OptimizeFinalizedSlicedMesh)
            {
                SlicedMesh.Optimize();

                // upload the mesh if it has been optimized
                SlicedMesh.UploadMeshData(SlicerConfiguration.SetFinalizedSlicedMeshToBeNoLongerReadable);
            }
            else if (SlicerConfiguration.SetFinalizedSlicedMeshToBeNoLongerReadable)
            {
                // If we are not optimizing the mesh, only upload if the user has enabled the setting to set meshes to be no longer readable
                SlicedMesh.UploadMeshData(true);
            }
        }

        /// <summary>
        /// Resets the mesh hashes
        /// </summary>
        public void ResetHashes()
        {
            SlicedVertHash = HashUtility.CalculateHash(Time.frameCount * 3);
            SlicedUvHash = HashUtility.CalculateHash(Time.frameCount * 5);
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

            if (MeshFilter != null)
            {
                if (MeshFilter.sharedMesh != null)
                {
                    var tempHash = HashUtility.CalculateHash(MeshFilter.sharedMesh);
                    HashUtility.AppendHash(tempHash, ref hash);
                }
            }

            if (MeshRenderer != null)
            {
                var tempHash = HashUtility.CalculateHash(MeshRenderer.enabled, 5);
                HashUtility.AppendHash(tempHash, ref hash);

                foreach (var material in MeshRenderer.sharedMaterials)
                {
                    if (material != null)
                    {

                        var matHash = HashUtility.CalculateHash(material);
                        HashUtility.AppendHash(matHash, ref hash);

                        var mainTexture = material.mainTexture;
                        if (mainTexture != null)
                        {
                            var mainTextureHash = HashUtility.CalculateHash(mainTexture);
                            var wrapModeUHash = HashUtility.CalculateHash(mainTexture.wrapModeU);
                            var wrapModeVHash = HashUtility.CalculateHash(mainTexture.wrapModeV);
                            HashUtility.AppendHash(mainTextureHash, wrapModeUHash, wrapModeVHash, ref hash);
                        }
                    }
                }
            }

            return hash;
        }
    }
}

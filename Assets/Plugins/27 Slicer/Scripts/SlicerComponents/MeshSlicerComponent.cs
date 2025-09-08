// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// This component will search for any MeshFilters that are a descendant of this component. It will then slice any meshes that are assigned to the found MeshFilters.
    /// </summary>
    /// <example>
    /// <br /><br />[REFERENCE MANUAL](xref:manual\components\mesh_slicer_component)
    /// </example>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Slicer/Mesh Slicer Component")]
    [HelpURL(SlicerConfiguration.SiteUrl + SlicerConfiguration.ComponentsManualPath + "mesh_slicer_component.html")]
    public class MeshSlicerComponent : SlicerComponent
    {
        /// <summary>
        /// Should the vertices of the mesh be sliced.
        /// </summary>
        /// <value><c>false</c> by default.</value>
        [Tooltip("Should the vertices of the mesh be sliced.")]
        public bool SkipVertices = false;

        /// <summary>
        /// Should the UVs of the mesh be sliced.
        /// </summary>
        /// <value><c>true</c> by default.</value>
        [Tooltip("Should the UVs of the mesh be sliced.")]
        public bool SkipUvs = true;

        public UvMappingSettings UvMappingSettings = new UvMappingSettings();

        [HideInInspector]
        [SerializeField]
        private List<MeshDetails> meshDetailsList = new List<MeshDetails>();

        /// <summary>
        /// A read only collection of Mesh Renders and Filters that are being managed by this <see cref="MeshSlicerComponent"/>.
        /// </summary>
        public ReadOnlyCollection<MeshDetails> MeshDetailsList { get { return meshDetailsList.AsReadOnly(); } }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                foreach (var md in meshDetailsList)
                {
                    if (md == null)
                    {
                        continue;
                    }

                    md.Destroy();
                }
            }
        }

        /// <inheritdoc/>
        public override void PreGatherDetails()
        {
            foreach (var md in meshDetailsList)
            {
                if (md == null)
                {
                    continue;
                }

                md.Remove = true;
            }
        }

        /// <inheritdoc/>
        public override void GatherDetails(Transform childTransform, Transform rootTransform)
        {
            var childMeshFilter = childTransform.GetComponent<MeshFilter>();
            var childMeshRenderer = childTransform.GetComponent<MeshRenderer>();


            if (childMeshFilter == null || childMeshRenderer == null || childMeshRenderer.enabled == false)
            {
                return;
            }

            MeshDetails md = meshDetailsList.FirstOrDefault(e =>
            {
                if (e == null)
                {
                    return false;
                }

                if (e.Transform == childTransform)
                {
                    return true;
                }

                if (e.Id == childMeshFilter.GetInstanceID())
                {
                    return true;
                }
                
                
                return false;
            });

            if (md == null)
            {
                if (childMeshFilter.sharedMesh == null)
                {
                    return;
                }

                md = new MeshDetails();
                md.Id = childMeshFilter.GetInstanceID();
                md.Transform = childTransform;
                md.MeshFilter = childMeshFilter;
                md.MeshRenderer = childMeshRenderer;

                var copiedModifiedMesh = meshDetailsList.FirstOrDefault(e => e.SlicedMesh == childMeshFilter.sharedMesh);
                if (copiedModifiedMesh != null && copiedModifiedMesh.OriginalSharedMesh != null)
                {
                    md.OriginalSharedMesh = copiedModifiedMesh.OriginalSharedMesh;
                }
                else
                {
                    var slicedMeshInParent = GetSlicedMeshInParent(rootTransform, childMeshFilter.sharedMesh);
                    if (slicedMeshInParent != null)
                    {
                        md.OriginalSharedMesh = slicedMeshInParent;
                    }
                    else
                    {
                        md.OriginalSharedMesh = childMeshFilter.sharedMesh;
                    }
                }

                if (md.SlicedMesh == null && md.OriginalSharedMesh != null && md.OriginalSharedMesh.isReadable)
                {
                    CopySharedMeshToSlicedMesh(md);
                }

                meshDetailsList.Add(md);
            }
            else if (md.Id != childMeshFilter.GetInstanceID() ||
                     (md.Id == childMeshFilter.GetInstanceID() && md.Transform == null))
            {
                md.Id = childMeshFilter.GetInstanceID();
                md.Transform = childTransform;
                md.MeshFilter = childMeshFilter;
                md.MeshRenderer = childMeshRenderer;

                if (md.OriginalSharedMesh != null && md.OriginalSharedMesh.isReadable)
                {
                    CopySharedMeshToSlicedMesh(md);
                }
            }
            else if (((md.SlicedMesh != md.MeshFilter.sharedMesh && SlicingEnabled) || // The user just replaced the mesh while slicing is enabled OR
                    (md.MeshFilter.sharedMesh != md.OriginalSharedMesh && !SlicingEnabled)) && // The user just replaced the mesh while slicing is disabled
                    (md.MeshFilter.sharedMesh == null || md.MeshFilter.sharedMesh.isReadable))
            {
                md.OriginalSharedMesh = md.MeshFilter.sharedMesh;
                CopySharedMeshToSlicedMesh(md);
            }
            else if (md.SlicedMesh == null && md.OriginalSharedMesh != null && md.OriginalSharedMesh.isReadable)
            {
                // The mesh details has a original mesh, but no mesh to slice
                CopySharedMeshToSlicedMesh(md);
            }

            if (md.OriginalSharedMesh == null)
            {
                return;
            }

            if (md.OriginalSharedMesh.isReadable)
            {
                md.OriginalBounds = MeshUtility.CalculateBounds(md.OriginalSharedMesh, md.Transform, rootTransform);
            }

            if (SlicingEnabled)
            {
                if (md.MeshFilter.sharedMesh != md.SlicedMesh)
                {
                    md.EnableSlicing();
                }
            }
            else
            {
                if (md.MeshFilter.sharedMesh != md.OriginalSharedMesh)
                {
                    md.DisableSlicing();
                }
            }

            md.Remove = false;
        }

        private Mesh GetSlicedMeshInParent(Transform transform, Mesh mesh)
        {
            var parentMeshSlicer = GetMeshSlicerInParent(transform);
            if (parentMeshSlicer != null)
            {
                var copiedModifiedMesh = parentMeshSlicer.meshDetailsList.FirstOrDefault(e => e.SlicedMesh == mesh);
                if (copiedModifiedMesh != null && copiedModifiedMesh.OriginalSharedMesh != null)
                {
                    return copiedModifiedMesh.OriginalSharedMesh;
                }
            }

            return null;
        }

        private MeshSlicerComponent GetMeshSlicerInParent(Transform transform)
        {
            var parentTransform = transform.parent;

            if (parentTransform == null)
            {
                return null;
            }

            var meshSlicer = parentTransform.GetComponent<MeshSlicerComponent>();

            if (meshSlicer != null)
            {
                return meshSlicer;
            }

            return GetMeshSlicerInParent(parentTransform);
        }

        private void CopySharedMeshToSlicedMesh(MeshDetails md)
        {
            md.DestroySlicedMesh();

            if (md.OriginalSharedMesh == null)
            {
                md.SlicedMesh = null;
            }
            else
            {
                md.SlicedMesh = MeshUtility.CopyMesh(md.OriginalSharedMesh);
            }

            md.ResetHashes();
        }

        /// <inheritdoc/>
        public override Hash128 PostGatherDetails()
        {
            var hash = base.PostGatherDetails();
            var skipVertHash = HashUtility.CalculateHash(SkipVertices, 3);
            var skipUvHash = HashUtility.CalculateHash(SkipUvs, 4);
            var uvMappingHash = UvMappingSettings.CalculateHash();
            HashUtility.AppendHash(uvMappingHash, skipVertHash, skipUvHash, ref hash);

            for (int i = meshDetailsList.Count - 1; i >= 0; i--)
            {
                var md = meshDetailsList[i];

                if (md == null)
                {
                    meshDetailsList.RemoveAt(i);
                    continue;
                }

                if (!md.Remove)
                {
                    var tempHash = md.CalculateHash();
                    HashUtility.AppendHash(tempHash, ref hash);

                    continue;
                }

                md.DisableSlicing();
                md.Destroy();

                meshDetailsList.RemoveAt(i);
            }

            return hash;
        }

        /// <inheritdoc/>
        public override Bounds? CalculateBounds()
        {
            if (SkipBoundsCalculation)
            {
                return null;
            }

            Bounds? encapsulatedBounds = null;
            foreach (var md in meshDetailsList)
            {
                encapsulatedBounds = BoundsUtility.Encapsulate(encapsulatedBounds, md.OriginalBounds);
            }

            return encapsulatedBounds;
        }

        /// <inheritdoc/>
        public override void Slice(Vector3 size, Transform rootTransform, Bounds completeBounds, Bounds slicedBounds, Vector3 slices)
        {
            foreach (var md in meshDetailsList)
            {
                if (md.OriginalSharedMesh == null)
                {
                    continue;
                }

                if (!md.OriginalSharedMesh.isReadable)
                {
                    continue;
                }

                var vertHash = SliceUtility.SliceVerts(md.OriginalSharedMesh, md.SlicedMesh, md.Transform, size, rootTransform, completeBounds, slicedBounds, SkipVertices, md.SlicedVertHash);

                var uvHash = UvUtility.SliceUvs(md.OriginalSharedMesh, md.SlicedMesh, md.MeshRenderer, md.Transform, size, rootTransform, completeBounds, slicedBounds, slices, SkipUvs, md.SlicedUvHash, UvMappingSettings);

                if (vertHash != md.SlicedVertHash || uvHash != md.SlicedUvHash || !SlicerConfiguration.SkipUnmodifiedSlices)
                {
                    md.SlicedMesh.UploadMeshData(false);
                    md.SlicedMesh.RecalculateBounds();

                    md.SlicedVertHash = vertHash;
                    md.SlicedUvHash = uvHash;
                }
            }
        }

        /// <inheritdoc/>
        public override void DisableSlicing()
        {
            base.DisableSlicing();
            foreach (var md in meshDetailsList)
            {
                md.DisableSlicing();
            }
        }

        /// <inheritdoc/>
        public override void EnableSlicing()
        {
            base.EnableSlicing();
            foreach (var md in meshDetailsList)
            {
                md.EnableSlicing();
            }
        }

        /// <inheritdoc/>
        public override void FinalizeSlicing()
        {
            foreach (var meshDetail in meshDetailsList)
            {
                meshDetail.FinalizeSlicing();
            }
            meshDetailsList.Clear();

            SlicerController.SafeDestroy(this);
        }

        /// <summary>
        /// Gets the <see cref="MeshDetails"/> of a Mesh if it is being tracked by this MeshSlicerComponent.
        /// </summary>
        /// <param name="transform">the transform to search for.</param>
        /// <param name="mesh">the mesh to search for. this could be either the <see cref="MeshDetails.OriginalSharedMesh"/> or <see cref="MeshDetails.SlicedMesh"/>.</param>
        /// <returns>Returns the <see cref="MeshDetails"/> of the mesh, or null if it is not tracked by this MeshSlicerComponent.</returns>
        public MeshDetails GetMeshDetailsByMesh(Transform transform, Mesh mesh)
        {
            if (mesh == null)
            {
                return null;
            }

            foreach (var meshDetail in meshDetailsList)
            {
                if (meshDetail.Transform != transform)
                {
                    continue;
                }
                
                if (meshDetail.OriginalSharedMesh == mesh || meshDetail.SlicedMesh == mesh)
                {
                    return meshDetail;
                }

                // last ditch effort to match
                // Reverting a prefab results in a new mesh with a different id
                // So try and match it by name instead
                var meshName = mesh.name.Replace(" Editor ", " mode ").Replace(" Runtime ", " mode ");

                if (meshDetail.OriginalSharedMesh != null)
                {
                    var originalSharedMeshName = meshDetail.OriginalSharedMesh.name.Replace(" Editor ", " mode ").Replace(" Runtime ", " mode ");

                    if (meshName == originalSharedMeshName)
                    {
                        return meshDetail;
                    }
                }

                if (meshDetail.SlicedMesh != null)
                {
                    var slicedMeshName = meshDetail.SlicedMesh.name.Replace(" Editor ", " mode ").Replace(" Runtime ", " mode ");

                    if (meshName == slicedMeshName)
                    {
                        return meshDetail;
                    }
                }
            }

            return null;
        }
        /// <summary>
        /// Gets the <see cref="MeshDetails"/> of a Mesh if it is being tracked by this MeshSlicerComponent.
        /// </summary>
        /// <param name="transform">the transform to search for.</param>
        /// <returns>Returns the <see cref="MeshDetails"/> of the mesh, or null if it is not tracked by this MeshSlicerComponent.</returns>
        public MeshDetails GetMeshDetailsByTransform(Transform transform)
        {
            var md = meshDetailsList.FirstOrDefault(e => e.Transform == transform);
            return md;
        }
    }
}
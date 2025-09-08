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
    /// This component will search for any Colliders that are a descendant of this component. It will then slice any valid types of Collider.
    /// </summary>
    /// <example>
    /// <br /><br />[REFERENCE MANUAL](xref:manual\components\collider_slicer_component)
    /// </example>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Slicer/Collider Slicer Component")]
    [HelpURL(SlicerConfiguration.SiteUrl + SlicerConfiguration.ComponentsManualPath + "collider_slicer_component.html")]
    public class ColliderSlicerComponent : SlicerComponent
    {
        /// <summary>
        /// Should the vertices of the mesh colliders be sliced.
        /// </summary>
        /// <value><c>false</c> by default.</value>
        [Tooltip("Should the vertices of the mesh colliders be sliced.")]
        public bool SkipVertices = false;

        // Unity serialization has no support for polymorphism, as a work around we have a list of each derived type.
        // Lots of extra work needs to be done because of this.
        // 
        // https://docs.unity3d.com/2019.4/Documentation/Manual/script-Serialization.html
        [HideInInspector, SerializeField]
        private List<MeshColliderDetails> meshColliderDetailsList = new List<MeshColliderDetails>();
        [HideInInspector, SerializeField]
        private List<MeshSlicerManagedMeshColliderDetails> meshSlicerManagedMeshColliderDetailsList = new List<MeshSlicerManagedMeshColliderDetails>();
        [HideInInspector, SerializeField]
        private List<BoxColliderDetails> boxColliderDetailsList = new List<BoxColliderDetails>();
        [HideInInspector, SerializeField]
        private List<UnsupportedColliderDetails> unsupportedColliderDetailsList = new List<UnsupportedColliderDetails>();

        /// <summary>
        /// A read only collection of Mesh Colliders that are being managed by this <see cref="ColliderSlicerComponent"/>.
        /// </summary>
        public ReadOnlyCollection<MeshColliderDetails> MeshColliderDetailsList { get { return meshColliderDetailsList.AsReadOnly(); } }
        /// <summary>
        /// A read only collection of Mesh Colliders that are being tracked by this Collider Slicer and Managed by a <see cref="MeshSlicerComponent"/>.
        /// </summary>
        public ReadOnlyCollection<MeshSlicerManagedMeshColliderDetails> MeshSlicerManagedMeshColliderDetailsList { get { return meshSlicerManagedMeshColliderDetailsList.AsReadOnly(); } }
        /// <summary>
        /// A read only collection of Box Colliders that are being managed by this <see cref="ColliderSlicerComponent"/>.
        /// </summary>
        public ReadOnlyCollection<BoxColliderDetails> BoxColliderDetailsList { get { return boxColliderDetailsList.AsReadOnly(); } }
        /// <summary>
        /// A read only collection of Unsupported Colliders that are being tracked by this <see cref="ColliderSlicerComponent"/>.
        /// </summary>
        public ReadOnlyCollection<UnsupportedColliderDetails> UnsupportedColliderDetailsList { get { return unsupportedColliderDetailsList.AsReadOnly(); } }

        private IEnumerable<ColliderDetails> allColliderDetails => meshColliderDetailsList.Cast<ColliderDetails>()
            .Concat(meshSlicerManagedMeshColliderDetailsList)
            .Concat(boxColliderDetailsList)
            .Concat(unsupportedColliderDetailsList);

        private MeshSlicerComponent meshSlicerSibling;

        private void Awake()
        {
            meshSlicerSibling = GetComponent<MeshSlicerComponent>();
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                foreach (var cd in allColliderDetails)
                {
                    if (cd == null)
                    {
                        continue;
                    }

                    cd.Destroy();
                }
            }
        }

        /// <inheritdoc/>
        public override void PreGatherDetails()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                meshSlicerSibling = GetComponent<MeshSlicerComponent>();
            }
#endif

            foreach (var cd in allColliderDetails)
            {
                if (cd == null)
                {
                    continue;
                }

                cd.Remove = true;
            }
        }

        /// <inheritdoc/>
        public override void GatherDetails(Transform childTransform, Transform rootTransform)
        {
            TempCollections.Colliders.Clear();
            childTransform.GetComponents(TempCollections.Colliders);

            foreach (var childCollider in TempCollections.Colliders)
            {
                GatherDetails(childCollider, childTransform, rootTransform);
            }

            TempCollections.Colliders.Clear();
        }

        private void GatherDetails(Collider childCollider, Transform childTransform, Transform rootTransform)
        {
            if (childCollider == null || childCollider.enabled == false)
            {
                return;
            }

            if (childCollider is MeshCollider meshCollider)
            {
                var mcd = GetColliderDetails(meshColliderDetailsList, childCollider, childTransform);
                var msmmcd = GetColliderDetails(meshSlicerManagedMeshColliderDetailsList, childCollider, childTransform);

                if (msmmcd != null && meshSlicerSibling == null)
                {
                    // This collider was previously managed by a mesh slicer, but it was probably destroyed.
                    if (msmmcd.MeshSlicerDetails != null)
                    {
                        meshCollider.sharedMesh = msmmcd.MeshSlicerDetails.OriginalSharedMesh;
                    }

                    msmmcd = null;
                }
                else if (msmmcd?.MeshSlicerDetails != null && msmmcd.MeshSlicerDetails.OriginalSharedMesh != null && msmmcd.MeshSlicerDetails.SlicedMesh != null)
                {
                    // Do Nothing
                }
                else if (meshCollider.sharedMesh == null)
                {
                    return;
                }

                Mesh meshToSearchWith;
                if (msmmcd != null && msmmcd.MeshSlicerDetails != null)
                {
                    if (meshCollider.sharedMesh != null && msmmcd.MeshSlicerDetails.SlicedMesh != msmmcd.MeshCollider.sharedMesh && SlicingEnabled)
                    {
                        meshToSearchWith = meshCollider.sharedMesh;
                    }
                    else if (meshCollider.sharedMesh != null && msmmcd.MeshSlicerDetails.OriginalSharedMesh != msmmcd.MeshCollider.sharedMesh && !SlicingEnabled)
                    {
                        meshToSearchWith = meshCollider.sharedMesh;
                    }
                    else
                    {
                        meshToSearchWith = msmmcd.MeshSlicerDetails.OriginalSharedMesh;
                    }
                }
                else if (mcd != null)
                {
                    if (mcd.SlicedMesh != mcd.MeshCollider.sharedMesh && SlicingEnabled)
                    {
                        meshToSearchWith = meshCollider.sharedMesh;
                    }
                    else if (mcd.OriginalSharedMesh != mcd.MeshCollider.sharedMesh && !SlicingEnabled)
                    {
                        meshToSearchWith = meshCollider.sharedMesh;
                    }
                    else
                    {
                        meshToSearchWith = mcd.OriginalSharedMesh;
                    }
                }
                else
                {
                    var slicedMeshInParent = GetSlicedMeshInParent(rootTransform, meshCollider.sharedMesh);

                    if (slicedMeshInParent != null)
                    {
                        meshToSearchWith = slicedMeshInParent;
                    }
                    else
                    {
                        meshToSearchWith = meshCollider.sharedMesh;
                    }
                }

                MeshDetails md = null;
                if (meshSlicerSibling != null)
                {
                    // See if there is already a Mesh Slicer that is managing this mesh
                    md = meshSlicerSibling.GetMeshDetailsByMesh(childTransform, meshToSearchWith);

                    if ((md != null && md.MeshRenderer != null && !md.MeshRenderer.enabled) || (meshSlicerSibling != null && !meshSlicerSibling.SlicingEnabled && SlicingEnabled))
                    {
                        // This collider was previously managed by a mesh slicer, but it is not anymore.
                        meshCollider.sharedMesh = md.OriginalSharedMesh;
                        md = null;
                    }
                }

                if (md == null)
                {
                    MeshColliderDetails_GatherChildDetails(mcd, meshCollider, childTransform, rootTransform);
                }
                else
                {
                    MeshSlicerManagedMeshCollider_GatherChildDetails(msmmcd, md, meshCollider, childTransform);
                }
            }
            else if (childCollider is BoxCollider boxCollider)
            {
                var bcd = GetColliderDetails(boxColliderDetailsList, boxCollider, childTransform);
                BoxColliderDetails_GatherChildDetails(bcd, boxCollider, childTransform, rootTransform);
            }
            else
            {
                var ucd = GetColliderDetails(unsupportedColliderDetailsList, childCollider, childTransform);
                UnsupportedColliderDetails_GatherChildDetails(ucd, childCollider, childTransform);
            }
        }

        private static MeshColliderDetails GetColliderDetails(List<MeshColliderDetails> colliderDetailsList, Collider childCollider, Transform childTransform)
        {
            var cd = colliderDetailsList.FirstOrDefault(e => e != null && e.Transform == childTransform && e.MeshCollider == childCollider);
            return cd;
        }

        private static MeshSlicerManagedMeshColliderDetails GetColliderDetails(List<MeshSlicerManagedMeshColliderDetails> colliderDetailsList, Collider childCollider, Transform childTransform)
        {
            var cd = colliderDetailsList.FirstOrDefault(e => e != null && e.Transform == childTransform && e.MeshCollider == childCollider);
            return cd;
        }

        private static BoxColliderDetails GetColliderDetails(List<BoxColliderDetails> colliderDetailsList, Collider childCollider, Transform childTransform)
        {
            var cd = colliderDetailsList.FirstOrDefault(e => e != null && e.Transform == childTransform && e.BoxCollider == childCollider);
            return cd;
        }

        private static UnsupportedColliderDetails GetColliderDetails(List<UnsupportedColliderDetails> colliderDetailsList, Collider childCollider, Transform childTransform)
        {
            var cd = colliderDetailsList.FirstOrDefault(e => e != null && e.Transform == childTransform && (e.Collider == childCollider));
            return cd;
        }

        /// <summary>
        /// Gathers details for Mesh Collider whose mesh is managed by a Mesh Slicer already
        /// </summary>
        private void MeshSlicerManagedMeshCollider_GatherChildDetails(MeshSlicerManagedMeshColliderDetails mcd, MeshDetails md, MeshCollider meshCollider, Transform childTransform)
        {
            if (mcd == null)
            {
                mcd = new MeshSlicerManagedMeshColliderDetails();
                mcd.Id = meshCollider.GetInstanceID();
                mcd.Transform = childTransform;

                mcd.MeshCollider = meshCollider;
                mcd.MeshSlicerDetails = md;

                meshSlicerManagedMeshColliderDetailsList.Add(mcd);
            }
            else if (mcd.Id != meshCollider.GetInstanceID())
            {
                mcd.Id = meshCollider.GetInstanceID();
                mcd.Transform = childTransform;

                mcd.MeshCollider = meshCollider;
                mcd.MeshSlicerDetails = md;

                mcd.ResetHashes();
            }
            else if (mcd.MeshSlicerDetails != md)
            {
                mcd.MeshSlicerDetails = md;

                mcd.ResetHashes();
            }

            if (mcd.MeshSlicerDetails != null && mcd.MeshSlicerDetails.OriginalBounds.HasValue)
            {
                mcd.OriginalBounds = mcd.MeshSlicerDetails.OriginalBounds.Value;
            }

            if (SlicingEnabled)
            {
                if (mcd.MeshCollider.sharedMesh != mcd.MeshSlicerDetails.SlicedMesh)
                {
                    mcd.EnableSlicing();
                }
            }
            else
            {
                if (mcd.MeshCollider.sharedMesh != mcd.MeshSlicerDetails.OriginalSharedMesh)
                {
                    mcd.DisableSlicing();
                }
            }

            mcd.Remove = false;
        }

        /// <summary>
        /// Gathers details for a Mesh Collider with its own distinct mesh
        /// </summary>
        private void MeshColliderDetails_GatherChildDetails(MeshColliderDetails mcd, MeshCollider meshCollider, Transform childTransform, Transform rootTransform)
        {
            if (mcd == null)
            {
                if (meshCollider.sharedMesh == null)
                {
                    return;
                }

                mcd = new MeshColliderDetails();
                mcd.Id = meshCollider.GetInstanceID();
                mcd.Transform = childTransform;
                mcd.MeshCollider = meshCollider;

                // See if the shared mesh for this model is set as modified mesh for a different model
                // This will happen if a model is duplicated or instanced
                var copiedModifiedMesh = meshColliderDetailsList.FirstOrDefault(e => e.SlicedMesh == meshCollider.sharedMesh);
                if (copiedModifiedMesh != null && copiedModifiedMesh.OriginalSharedMesh != null)
                {
                    mcd.OriginalSharedMesh = copiedModifiedMesh.OriginalSharedMesh;
                }
                else
                {
                    var slicedMeshInParent = GetSlicedMeshInParent(rootTransform, meshCollider.sharedMesh);
                    if (slicedMeshInParent != null)
                    {
                        mcd.OriginalSharedMesh = slicedMeshInParent;
                    }
                    else
                    {
                        mcd.OriginalSharedMesh = meshCollider.sharedMesh;
                    }
                }

                if (mcd.OriginalSharedMesh != null && mcd.OriginalSharedMesh.isReadable)
                {
                    CopySharedMeshToSlicedMesh(mcd);
                }

                meshColliderDetailsList.Add(mcd);
            }
            else if (mcd.Id != meshCollider.GetInstanceID())
            {
                mcd.Id = meshCollider.GetInstanceID();
                mcd.Transform = childTransform;

                mcd.MeshCollider = meshCollider;

                if (mcd.OriginalSharedMesh != null && mcd.SlicedMesh == null && mcd.OriginalSharedMesh.isReadable)
                {
                    CopySharedMeshToSlicedMesh(mcd);
                }
            }
            else if (((mcd.SlicedMesh != mcd.MeshCollider.sharedMesh && SlicingEnabled) || // The user just replaced the mesh while slicing is enabled
                    (mcd.MeshCollider.sharedMesh != mcd.OriginalSharedMesh && !SlicingEnabled)) && // The user just replaced the mesh while slicing is disabled
                    (mcd.MeshCollider.sharedMesh == null || mcd.MeshCollider.sharedMesh.isReadable))
            {
                mcd.OriginalSharedMesh = mcd.MeshCollider.sharedMesh;
                CopySharedMeshToSlicedMesh(mcd);
            }
            else if (mcd.SlicedMesh == null && mcd.OriginalSharedMesh != null && mcd.OriginalSharedMesh.isReadable)
            {
                // The mesh collider details has a original mesh, but no mesh to slice
                CopySharedMeshToSlicedMesh(mcd);
            }

            if (mcd.OriginalSharedMesh == null)
            {
                return;
            }

            if (mcd.OriginalSharedMesh.isReadable)
            {
                mcd.OriginalBounds = MeshUtility.CalculateBounds(mcd.OriginalSharedMesh, mcd.Transform, rootTransform);
            }

            if (SlicingEnabled)
            {
                if (mcd.MeshCollider.sharedMesh != mcd.SlicedMesh)
                {
                    mcd.EnableSlicing();
                }
            }
            else
            {
                if (mcd.MeshCollider.sharedMesh != mcd.OriginalSharedMesh)
                {
                    mcd.DisableSlicing();
                }
            }

            mcd.Remove = false;
        }

        private Mesh GetSlicedMeshInParent(Transform transform, Mesh mesh)
        {
            var parentColliderSlicer = GetColliderSlicerInParent(transform);
            if (parentColliderSlicer != null)
            {
                var mcd = parentColliderSlicer.meshColliderDetailsList.FirstOrDefault(e => e.SlicedMesh == mesh);
                if (mcd != null && mcd.OriginalSharedMesh != null)
                {
                    return mcd.OriginalSharedMesh;
                }

                var msmmcd = parentColliderSlicer.meshSlicerManagedMeshColliderDetailsList.FirstOrDefault(e => e.MeshSlicerDetails.SlicedMesh == mesh);
                if (msmmcd != null && msmmcd.MeshSlicerDetails.OriginalSharedMesh != null)
                {
                    return msmmcd.MeshSlicerDetails.OriginalSharedMesh;
                }
            }

            return null;
        }

        private ColliderSlicerComponent GetColliderSlicerInParent(Transform transform)
        {
            var parentTransform = transform.parent;

            if (parentTransform == null)
            {
                return null;
            }

            var colliderSlicer = parentTransform.GetComponent<ColliderSlicerComponent>();

            if (colliderSlicer != null)
            {
                return colliderSlicer;
            }

            return GetColliderSlicerInParent(parentTransform);
        }

        private void CopySharedMeshToSlicedMesh(MeshColliderDetails mcd)
        {
            mcd.DestroySlicedMesh();

            if (mcd.OriginalSharedMesh == null)
            {
                mcd.SlicedMesh = null;
            }
            else
            {
                mcd.SlicedMesh = MeshUtility.CopyMesh(mcd.OriginalSharedMesh);
            }

            mcd.ResetHashes();
        }

        /// <summary>
        /// Gathers details for a Box Collider
        /// </summary>
        private void BoxColliderDetails_GatherChildDetails(BoxColliderDetails bcd, BoxCollider collider, Transform childTransform, Transform rootTransform)
        {
            if (bcd == null)
            {
                bcd = new BoxColliderDetails();
                bcd.Id = collider.GetInstanceID();
                bcd.Transform = childTransform;
                bcd.BoxCollider = collider;

                if (SlicingEnabled)
                {
                    // This collider was made outside of edit mode
                    // When unity makes a slicer it likes to auto fit the collider
                    // But that auto fit would be for the sliced version of the mesh
                    // We want to auto fit to the unsliced version

                    if (meshSlicerSibling != null && collider.center == Vector3.zero && collider.size == Vector3.one)
                    {
                        var md = meshSlicerSibling.GetMeshDetailsByTransform(childTransform);
                        if (md.OriginalSharedMesh)
                        {
                            collider.center = md.OriginalSharedMesh.bounds.center;
                            collider.size = md.OriginalSharedMesh.bounds.size;
                        }
                    }
                }

                bcd.OriginalColliderProperties = BoundsUtility.AsBounds(collider);

                boxColliderDetailsList.Add(bcd);
            }
            else if (bcd.BoxCollider != null && !SlicingEnabled)
            {
                bcd.OriginalColliderProperties = BoundsUtility.AsBounds(collider);
            }

            bcd.OriginalBounds = BoundsUtility.CalculateBounds(bcd.OriginalColliderProperties, childTransform, rootTransform);

            bcd.Remove = false;
        }

        /// <summary>
        /// Gathers details for a Unsupported Collider
        /// </summary>
        private void UnsupportedColliderDetails_GatherChildDetails(UnsupportedColliderDetails ucd, Collider collider, Transform childTransform)
        {
            if (ucd == null)
            {
                ucd = new UnsupportedColliderDetails();
                ucd.Id = collider.GetInstanceID();
                ucd.Transform = childTransform;
                ucd.Collider = collider;

                unsupportedColliderDetailsList.Add(ucd);
            }
            else if (ucd.Id != collider.GetInstanceID())
            {
                ucd.Id = collider.GetInstanceID();
                ucd.Transform = childTransform;
                ucd.Collider = collider;
            }
            else if (ucd.Collider != collider)
            {
                ucd.Collider = collider;
            }

            ucd.Remove = false;
        }

        /// <inheritdoc/>
        public override Hash128 PostGatherDetails()
        {
            var hash = base.PostGatherDetails();
            var skipVertHash = HashUtility.CalculateHash(SkipVertices, 3);
            HashUtility.AppendHash(skipVertHash, ref hash);

            Hash128 tempHash;

            tempHash = PostGatherDetails(meshColliderDetailsList);
            HashUtility.AppendHash(tempHash, ref hash);

            tempHash = PostGatherDetails(meshSlicerManagedMeshColliderDetailsList);
            HashUtility.AppendHash(tempHash, ref hash);

            tempHash = PostGatherDetails(boxColliderDetailsList);
            HashUtility.AppendHash(tempHash, ref hash);

            tempHash = PostGatherDetails(unsupportedColliderDetailsList);
            HashUtility.AppendHash(tempHash, ref hash);

            return hash;
        }

        private static Hash128 PostGatherDetails<T>(List<T> colliderDetailsList) where T : ColliderDetails
        {
            Hash128 hash = new Hash128();

            for (int i = colliderDetailsList.Count - 1; i >= 0; i--)
            {
                var cd = colliderDetailsList[i];
                if (cd == null)
                {
                    colliderDetailsList.RemoveAt(i);
                    continue;
                }

                if (!cd.Remove)
                {
                    var tempHash = cd.CalculateHash();
                    HashUtility.AppendHash(tempHash, ref hash);

                    continue;
                }

                cd.DisableSlicing();
                cd.Destroy();

                colliderDetailsList.RemoveAt(i);
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
            foreach (var md in allColliderDetails)
            {
                encapsulatedBounds = BoundsUtility.Encapsulate(encapsulatedBounds, md.OriginalBounds);
            }

            return encapsulatedBounds;
        }

        /// <inheritdoc/>
        public override void Slice(Vector3 size, Transform rootTransform, Bounds completeBounds, Bounds slicedBounds, Vector3 slices)
        {
            foreach (var msmmcd in meshSlicerManagedMeshColliderDetailsList)
            {
                if (msmmcd.MeshCollider == null || msmmcd.MeshSlicerDetails == null)
                {
                    continue;
                }

                if (msmmcd.LastVertHash == msmmcd.MeshSlicerDetails.SlicedVertHash && SlicerConfiguration.SkipUnmodifiedSlices)
                {
                    continue;
                }

                msmmcd.MeshCollider.sharedMesh = msmmcd.MeshSlicerDetails.SlicedMesh;
                msmmcd.LastVertHash = msmmcd.MeshSlicerDetails.SlicedVertHash;
            }

            foreach (var mcd in meshColliderDetailsList)
            {
                if (mcd.OriginalSharedMesh == null)
                {
                    continue;
                }

                if (!mcd.OriginalSharedMesh.isReadable)
                {
                    continue;
                }

                var vertHash = SliceUtility.SliceVerts(mcd.OriginalSharedMesh, mcd.SlicedMesh, mcd.Transform, size, rootTransform, completeBounds, slicedBounds, SkipVertices, mcd.SlicedVertHash);

                if (vertHash != mcd.SlicedVertHash || !SlicerConfiguration.SkipUnmodifiedSlices)
                {
                    mcd.SlicedMesh.RecalculateBounds();

                    mcd.MeshCollider.sharedMesh = mcd.SlicedMesh;

                    mcd.SlicedVertHash = vertHash;
                }
            }

            foreach (var bcd in boxColliderDetailsList)
            {
                bcd.SlicedColliderProperties = SliceUtility.SliceVerts(bcd.OriginalColliderProperties, bcd.Transform, size, rootTransform, completeBounds, slicedBounds);

                bcd.BoxCollider.center = bcd.SlicedColliderProperties.center;
                bcd.BoxCollider.size = bcd.SlicedColliderProperties.size;
            }
        }

        /// <inheritdoc/>
        public override void DisableSlicing()
        {
            base.DisableSlicing();
            foreach (var cd in allColliderDetails)
            {
                cd.DisableSlicing();
            }
        }

        /// <inheritdoc/>
        public override void EnableSlicing()
        {
            base.EnableSlicing();
            foreach (var cd in allColliderDetails)
            {
                cd.EnableSlicing();
            }
        }

        /// <inheritdoc/>
        public override void FinalizeSlicing()
        {
            foreach (var colliderDetail in meshColliderDetailsList)
            {
                colliderDetail.FinalizeSlicing();
            }
            meshColliderDetailsList.Clear();

            foreach (var colliderDetail in meshSlicerManagedMeshColliderDetailsList)
            {
                colliderDetail.FinalizeSlicing();
            }
            meshSlicerManagedMeshColliderDetailsList.Clear();

            foreach (var colliderDetail in boxColliderDetailsList)
            {
                colliderDetail.FinalizeSlicing();
            }
            boxColliderDetailsList.Clear();

            foreach (var colliderDetail in unsupportedColliderDetailsList)
            {
                colliderDetail.FinalizeSlicing();
            }
            unsupportedColliderDetailsList.Clear();

            SlicerController.SafeDestroy(this);
        }
    }
}
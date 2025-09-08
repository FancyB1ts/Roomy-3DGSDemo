// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Slicer.Core
{
    /// <summary>
    /// Contains useful methods for slicing many types of objects.
    /// </summary>
    public static class SliceUtility
    {
        /// <summary>
        /// The hash that is to be used when Verts are not being sliced.
        /// </summary>
        public static readonly Hash128 SkipVertHash = HashUtility.CalculateHash(94771);
        
        /// <summary>
        /// The hash that is to be used when UVs are not being sliced.
        /// </summary>
        public static readonly Hash128 SkipUvHash = HashUtility.CalculateHash(63857);

        /// <summary>
        /// Slices a bounds as if it is made up of discrete vertices.
        /// </summary>
        /// <param name="origBounds">The bounds to slice</param>
        /// <param name="boundsTransform">The transform of the GameObject that contains the bounds</param>
        /// <param name="size">The final size (as a scale) of all the items that are to be sliced. See <c>SlicerController.Size</c> for more information.</param>
        /// <param name="rootTransform">The transform of the parent <c>SlicerController"</c></param>
        /// <param name="completeBounds">The complete bounding box of all sliced items after being sliced. See <c>SlicerController.CompleteBounds</c> for more information.</param>
        /// <param name="slicedBounds">The bounding box of the slices that will be made. See <c>SlicerController.SlicedBounds</c> for more information.</param>
        /// <returns>Returns the sliced bounds</returns>
        public static Bounds SliceVerts(Bounds origBounds, Transform boundsTransform, Vector3 size, Transform rootTransform, Bounds completeBounds, Bounds slicedBounds)
        {
            BoundsUtility.GetVerts(origBounds, TempCollections.Vector3);

            var matrix = MatrixUtility.BuildTransformMatrix(boundsTransform, rootTransform);

            Vector3 completeBoundsCenter = completeBounds.center;
            Vector3 completeBoundsExtents = completeBounds.extents;
            Vector3 slicedBoundsExtents = slicedBounds.extents;

            for (int i = 0; i < TempCollections.Vector3.Count; i++)
            {
                var vert = TempCollections.Vector3[i];
                vert = SliceVector(vert, matrix, size, completeBoundsCenter, completeBoundsExtents, slicedBoundsExtents);
                TempCollections.Vector3[i] = vert;
            }

            var bounds = BoundsUtility.Encapsulate(TempCollections.Vector3);

            return bounds;
        }

        /// <summary>
        /// Slices the vertices of the supplied <c>origSharedMesh</c> and applies it to the supplied <c>modifiedMesh</c>.
        /// </summary>
        /// <param name="origSharedMesh">The original mesh.</param>
        /// <param name="modifiedMesh">The mesh the slicing will be applied to.</param>
        /// <param name="meshTransform">The transform of the GameObject that contains the mesh.</param>
        /// <param name="size">The final size (as a scale) of all the items that are to be sliced. See <c>SlicerController.Size</c> for more information.</param>
        /// <param name="rootTransform">The transform of the parent <c>SlicerController"</c></param>
        /// <param name="completeBounds">The complete bounding box of all sliced items after being sliced. See <c>SlicerController.CompleteBounds</c> for more information.</param>
        /// <param name="slicedBounds">The bounding box of the slices that will be made. See <c>SlicerController.SlicedBounds</c> for more information.</param>
        /// <param name="skipVertices">Should the verts be sliced, if true the vertices will not be sliced.</param>
        /// <param name="previousHash">The previous hash from slicing, used to determine if any changes should be uploaded to the GPU etc.</param>
        /// <returns>Returns the hash of the sliced verts, or <see cref="SkipVertHash"/> if <c>skipVertices</c> is set to <c>true</c>.</returns>
        public static Hash128 SliceVerts(Mesh origSharedMesh, Mesh modifiedMesh, Transform meshTransform, Vector3 size, Transform rootTransform, Bounds completeBounds, Bounds slicedBounds, bool skipVertices, Hash128 previousHash)
        {
            origSharedMesh.GetVertices(TempCollections.Vector3);

            if (skipVertices)
            {
                // if the previous hash is already skipVertHash then we have already skipped prior, no need to do it again
                if (previousHash != SkipVertHash || !SlicerConfiguration.SkipUnmodifiedSlices)
                {
                    modifiedMesh.SetVertices(TempCollections.Vector3);
                }

                return SkipVertHash;
            }

            var matrix = MatrixUtility.BuildTransformMatrix(meshTransform, rootTransform);

            Vector3 completeBoundsCenter = completeBounds.center;
            Vector3 completeBoundsExtents = completeBounds.extents;
            Vector3 slicedBoundsCenter = slicedBounds.center;
            Vector3 slicedBoundsExtents = slicedBounds.extents;

            Vector3 offset = slicedBoundsCenter - completeBoundsCenter;
            completeBoundsCenter = slicedBoundsCenter;

            Hash128 hash;
            {
                // The following hashes should be enough to determine if the vertices are going to be changed
                // It assumes that the verts in origSharedMesh is not changed.
                // If the verts are modified, slightly tweaking the first vert will be enough to change the hash

                var origSharedMeshHash = HashUtility.CalculateHash(origSharedMesh);
                var matrixHash = HashUtility.CalculateHash(matrix);
                var sizeHash = HashUtility.CalculateHash(size);
                var completeBoundsCenterHash = HashUtility.CalculateHash(completeBoundsCenter);
                var completeBoundsExtentsHash = HashUtility.CalculateHash(completeBoundsExtents);
                var slicedBoundsExtentsHash = HashUtility.CalculateHash(slicedBoundsExtents);
                var offsetHash = HashUtility.CalculateHash(offset);
                HashUtility.AppendHash(origSharedMeshHash, matrixHash, sizeHash, completeBoundsCenterHash, completeBoundsExtentsHash, offsetHash, ref slicedBoundsExtentsHash);
                hash = slicedBoundsExtentsHash;

                if (TempCollections.Vector3.Count > 0)
                {
                    var firstVertHash = HashUtility.CalculateHash(TempCollections.Vector3[0]);
                }
            }

            if (hash == previousHash && SlicerConfiguration.SkipUnmodifiedSlices)
            {
                return hash;
            }

            for (int i = 0; i < TempCollections.Vector3.Count; i++)
            {
                var vert = TempCollections.Vector3[i];
                vert += offset;
                vert = SliceVector(vert, matrix, size, completeBoundsCenter, completeBoundsExtents, slicedBoundsExtents);
                TempCollections.Vector3[i] = vert;
            }

            modifiedMesh.SetVertices(TempCollections.Vector3);

            return hash;
        }

        /// <summary>
        /// Slices a single vector after transforming it by the supplied matrix.
        /// </summary>
        /// <param name="vert">The vector to apply slicing to.</param>
        /// <param name="matrix">The matrix to apply to the vector before slicing.</param>
        /// <param name="size">The final size (as a scale) of all the items that are to be sliced. See <c>SlicerController.Size</c> for more information.</param>
        /// <param name="completeBoundsCenter">The center vector of the complete bounding box of all sliced items after being sliced. See <c>SlicerController.CompleteBounds</c> for more information.</param>
        /// <param name="completeBoundsExtents">The extents vector of the complete bounding box of all sliced items after being sliced. See <c>SlicerController.CompleteBounds</c> for more information.</param>
        /// <param name="slicedBoundsExtents">The extents vector of the bounding box of the slices that will be made. See <c>SlicerController.SlicedBounds</c> for more information.</param>
        /// <returns>Returns the sliced vector.</returns>
        public static Vector3 SliceVector(Vector3 vert, Matrix4x4 matrix, Vector3 size, Vector3 completeBoundsCenter, Vector3 completeBoundsExtents, Vector3 slicedBoundsExtents)
        {
            var transformedVert = VectorUtility.TransformVector(vert, matrix);
            transformedVert = SliceVector(transformedVert, size, completeBoundsCenter, completeBoundsExtents, slicedBoundsExtents);
            vert = VectorUtility.TransformVector(transformedVert, matrix.inverse);

            return vert;
        }

        /// <summary>
        /// Slices a single vector.
        /// </summary>
        /// <param name="vert">The vector to apply slicing to.</param>
        /// <param name="size">The final size (as a scale) of all the items that are to be sliced. See <c>SlicerController.Size</c> for more information.</param>
        /// <param name="completeBounds">The complete bounding box of all sliced items after being sliced. See <c>SlicerController.CompleteBounds</c> for more information.</param>
        /// <param name="slicedBounds">The bounding box of the slices that will be made. See <c>SlicerController.SlicedBounds</c> for more information.</param>
        /// <returns>Returns the sliced vector.</returns>
        public static Vector3 SliceVector(Vector3 vert, Vector3 size, Bounds completeBounds, Bounds slicedBounds)
        {
            return SliceVector(vert, size, completeBounds.center, completeBounds.extents, slicedBounds.extents);
        }

        /// <summary>
        /// Slices a single vector.
        /// </summary>
        /// <param name="vert">The vector to apply slicing to.</param>
        /// <param name="size">The final size (as a scale) of all the items that are to be sliced. See <c>SlicerController.Size</c> for more information.</param>
        /// <param name="completeBoundsCenter">The center vector of the complete bounding box of all sliced items after being sliced. See <c>SlicerController.CompleteBounds</c> for more information.</param>
        /// <param name="completeBoundsExtents">The extents vector of the complete bounding box of all sliced items after being sliced. See <c>SlicerController.CompleteBounds</c> for more information.</param>
        /// <param name="slicedBoundsExtents">The extents vector of the bounding box of the slices that will be made. See <c>SlicerController.SlicedBounds</c> for more information.</param>
        /// <returns>Returns the sliced vector.</returns>
        public static Vector3 SliceVector(Vector3 vert, Vector3 size, Vector3 completeBoundsCenter, Vector3 completeBoundsExtents, Vector3 slicedBoundsExtents)
        {
            vert.x = SliceSingleVectorDimension(0, vert, size, completeBoundsCenter, completeBoundsExtents, slicedBoundsExtents);
            vert.y = SliceSingleVectorDimension(1, vert, size, completeBoundsCenter, completeBoundsExtents, slicedBoundsExtents);
            vert.z = SliceSingleVectorDimension(2, vert, size, completeBoundsCenter, completeBoundsExtents, slicedBoundsExtents);

            return vert;
        }

        /// <summary>
        /// Slices a single dimension of a single vector.
        /// </summary>
        /// <param name="i">The dimension index of the supplied <c>transformedVertVec</c>.</param>
        /// <param name="transformedVertVec">The Vector that will have its single dimension sliced.</param>
        /// <param name="sizeVec">The final size (as a scale) of all the items that are to be sliced. See <c>SlicerController.Size</c> for more information.</param>
        /// <param name="completeBoundsCenterVec">The center vector of the complete bounding box of all sliced items after being sliced. See <c>SlicerController.CompleteBounds</c> for more information.</param>
        /// <param name="completeBoundsExtentsVec">The extents vector of the complete bounding box of all sliced items after being sliced. See <c>SlicerController.CompleteBounds</c> for more information.</param>
        /// <param name="slicedBoundsExtentsVec">The extents vector of the bounding box of the slices that will be made. See <c>SlicerController.SlicedBounds</c> for more information.</param>
        /// <returns>Returns the sliced vector dimension.</returns>
        private static float SliceSingleVectorDimension(int i, Vector3 transformedVertVec, Vector3 sizeVec, Vector3 completeBoundsCenterVec, Vector3 completeBoundsExtentsVec, Vector3 slicedBoundsExtentsVec)
        {
            float boundsCenter = completeBoundsCenterVec[i];
            float boundsExtents = completeBoundsExtentsVec[i];
            float slicedBoundsExtents = slicedBoundsExtentsVec[i];
            float size = sizeVec[i];

            var transformedVert = transformedVertVec[i];

            if (transformedVert <= boundsCenter - slicedBoundsExtents)
            {
                var offset = (boundsCenter - boundsExtents) - transformedVert;
                transformedVert = (boundsCenter - (boundsExtents * size)) - offset;
            }
            else if (transformedVert >= boundsCenter + slicedBoundsExtents)
            {
                var offset = (boundsCenter + boundsExtents) - transformedVert;
                transformedVert = (boundsCenter + (boundsExtents * size)) - offset;
            }
            else
            {
                var slicedBoundsMin = boundsCenter - slicedBoundsExtents;
                var slicedBoundsMax = boundsCenter + slicedBoundsExtents;

                var normalizedPos = (transformedVert - slicedBoundsMin) / (slicedBoundsMax - slicedBoundsMin);

                var scaledSlicedExtents = slicedBoundsExtents * size;
                transformedVert = Mathf.Lerp(boundsCenter - scaledSlicedExtents, boundsCenter + scaledSlicedExtents, normalizedPos);
            }

            return transformedVert;
        }
    }
}

// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Slicer.Core
{
    /// <summary>
    /// Contains useful methods for slicing meshes
    /// </summary>
    public static class MeshUtility
    {
        /// <summary>
        /// Creates a copy of a mesh.
        /// </summary>
        /// <param name="origMesh">The mesh to make a copy of.</param>
        /// <returns>Returns the copy of the mesh.</returns>
        public static Mesh CopyMesh(Mesh origMesh)
        {
            if (origMesh == null)
            {
                return null;
            }

            var newMesh = Mesh.Instantiate(origMesh);

            var mode = Application.isPlaying ? "Runtime" : "Editor";
            newMesh.name = $"{origMesh.name} {mode} Sliced";
            newMesh.MarkDynamic();

            return newMesh;
        }

        /// <summary>
        /// Creates a copy of a mesh.
        /// </summary>
        /// <param name="origMesh">The mesh to make a copy of.</param>
        /// <returns>Returns the copy of the mesh.</returns>
        public static void CopyMesh(Mesh sourceMesh, Mesh destMesh)
        {
            if (sourceMesh == null)
            {
                throw new ArgumentNullException(nameof(sourceMesh));
            }

            if (destMesh == null)
            {
                throw new ArgumentNullException(nameof(destMesh));
            }

            destMesh.Clear();

            sourceMesh.GetVertices(TempCollections.Vector3);
            destMesh.SetVertices(TempCollections.Vector3);

            sourceMesh.GetNormals(TempCollections.Vector3);
            destMesh.SetNormals(TempCollections.Vector3);

            CopyUVs(sourceMesh, destMesh);

            sourceMesh.GetColors(TempCollections.Colors);
            destMesh.SetColors(TempCollections.Colors);

            sourceMesh.GetTangents(TempCollections.Vector4);
            destMesh.SetTangents(TempCollections.Vector4);

            var subMeshCount = sourceMesh.subMeshCount;
            destMesh.subMeshCount = subMeshCount;
            for (int i = 0; i < subMeshCount; i++)
            {
                var meshTopology = sourceMesh.GetTopology(i);
                sourceMesh.GetIndices(TempCollections.Integers, i);
                destMesh.SetIndices(TempCollections.Integers, meshTopology, i);
            }
        }

        private static void CopyUVs(Mesh sourceMesh, Mesh destMesh)
        {
            // Reset the modified UVs back to the original
            for (int uvChannel = 0; uvChannel < 7; uvChannel++)
            {
                var uvDimension = sourceMesh.GetVertexAttributeDimension(uvChannel + VertexAttribute.TexCoord0);
                switch (uvDimension)
                {
                    case 2:
                        sourceMesh.GetUVs(uvChannel, TempCollections.Vector2);
                        destMesh.SetUVs(uvChannel, TempCollections.Vector2);
                        break;
                    case 3:
                        sourceMesh.GetUVs(uvChannel, TempCollections.Vector3);
                        destMesh.SetUVs(uvChannel, TempCollections.Vector3);
                        break;
                    case 4:
                        sourceMesh.GetUVs(uvChannel, TempCollections.Vector4);
                        destMesh.SetUVs(uvChannel, TempCollections.Vector4);
                        break;
                    default:
                        continue;
                }
            }
        }

        /// <summary>
        /// Converts a meshes bounds into a bounds in slicer space (local space of the parent SlicerComponent).
        /// </summary>
        /// <param name="mesh">The mesh to encapsulate into the returned bounds.</param>
        /// <param name="childTransform">The transform of the GameObject that contains the mesh.</param>
        /// <param name="rootTransform">The transform of the GameObject containing the <c>SlicerController</c>.</param>
        /// <returns>The bounds that contains the meshes transformed vectors.</returns>
        public static Bounds CalculateBounds(Mesh mesh, Transform childTransform, Transform rootTransform)
        {
            // This matrix converts the local mesh vertex dependent on the transform
            // position, scale and orientation into a global position
            var matrix = MatrixUtility.BuildTransformMatrix(childTransform, rootTransform);

            var origSharedMesh = mesh;
            origSharedMesh.GetVertices(TempCollections.Vector3);

            var transformedBounds = VectorUtility.CalculateBounds(TempCollections.Vector3, matrix);

            TempCollections.Vector3.Clear();
            return transformedBounds;
        }

        /// <summary>
        /// Resets the UVs in <c>destMesh</c> back to the UVs in <c>sourceMesh</c>
        /// </summary>
        /// <param name="sourceMesh">The source mesh</param>
        /// <param name="destMesh">The destination mesh</param>
        public static void ResetUV(Mesh sourceMesh, Mesh destMesh)
        {
            CopyUVs(sourceMesh, destMesh);
        }
    }
}

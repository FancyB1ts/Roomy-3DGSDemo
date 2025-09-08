// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Slicer.Core
{
    /// <summary>
    /// Contains useful methods for slicing many types of objects.
    /// </summary>
    public static class UvUtility
    {
        /// <summary>
        /// Slices the UVs of the supplied <c>origSharedMesh</c> and applies it to the supplied <c>modifiedMesh</c>.
        /// </summary>
        /// <param name="origSharedMesh">The original mesh.</param>
        /// <param name="modifiedMesh">The mesh the slicing will be applied to.</param>
        /// <param name="meshRenderer">The mesh render that is used to render the mesh</param>
        /// <param name="meshTransform">The transform of the GameObject that contains the mesh.</param>
        /// <param name="size">The final size (as a scale) of all the items that are to be sliced. See <c>SlicerController.Size</c> for more information.</param>
        /// <param name="rootTransform"></param>
        /// <param name="completeBounds"></param>
        /// <param name="slicedBounds"></param>
        /// <param name="vector3"></param>
        /// <param name="skipUvs">Should the UVs be sliced, if true the vertices will not be sliced.</param>
        /// <param name="previousHash">The previous hash from slicing, used to determine if any changes should be uploaded to the GPU etc.</param>
        /// <returns>Returns the hash of the sliced UVs, or <see cref="SkipUvHash"/> if <c>skipUvs</c> is set to <c>true</c>.</returns>
        public static Hash128 SliceUvs(Mesh origSharedMesh, Mesh modifiedMesh, MeshRenderer meshRenderer,
            Transform meshTransform, Vector3 size, Transform rootTransform, Bounds completeBounds, Bounds slicedBounds,
            Vector3 slices, bool skipUvs, Hash128 previousHash, UvMappingSettings uvMappingSettings)
        {
            if (skipUvs)
            {
                // if the previous hash is already skipUvHash then we have already skipped prior, no need to do it again
                if (previousHash != SliceUtility.SkipUvHash || !SlicerConfiguration.SkipUnmodifiedSlices)
                {
                    // Reset the UVs back tho their default if we want to skip them
                    MeshUtility.ResetUV(origSharedMesh, modifiedMesh);
                }

                return SliceUtility.SkipUvHash;
            }

            var sharedMaterials = TempCollections.Materials;

            meshRenderer.GetSharedMaterials(sharedMaterials);
            if (sharedMaterials.Count != origSharedMesh.subMeshCount)
            {
                MeshUtility.ResetUV(origSharedMesh, modifiedMesh);
                return new Hash128();
            }

            Hash128 hash;
            {
                // The following hashes should be enough to determine if the vertices are going to be changed
                // It assumes that the UVs in origSharedMesh is not changed.
                // If the UVs are modified, slightly tweaking the first UV of any UV channel will be enough to change the hash

                var uvMappingHash = uvMappingSettings.CalculateHash();
                var origSharedMeshHash = HashUtility.CalculateHash(origSharedMesh);
                var sizeHash = HashUtility.CalculateHash(size);
                HashUtility.AppendHash(uvMappingHash, origSharedMeshHash, ref sizeHash);
                hash = sizeHash;
            }
            
            var modifiedVertices = TempCollections.Vector3;
            modifiedMesh.GetVertices(modifiedVertices);

            for (int uvChannel = 0; uvChannel < 7; uvChannel++)
            {
                var hasUvChannel = origSharedMesh.HasVertexAttribute(VertexAttribute.TexCoord0 + uvChannel);
                if (!hasUvChannel)
                {
                    continue;
                }

                var uvs = TempCollections.Vector2;
                origSharedMesh.GetUVs(uvChannel, uvs);

                if (uvs.Count == 0)
                {
                    continue;
                }

                var firstUVHash = HashUtility.CalculateHash(uvs[0]);
                HashUtility.AppendHash(firstUVHash, ref hash);

                for (int subMeshIndex = 0; subMeshIndex < origSharedMesh.subMeshCount; subMeshIndex++)
                {
                    var material = sharedMaterials[subMeshIndex];
                    var mainTexture = material?.mainTexture;
                    
                    var subMesh = origSharedMesh.GetSubMesh(subMeshIndex);
                    var validTarget = ValidSliceTarget(mainTexture, subMesh);
                    if (!validTarget)
                    {
                        continue;
                    }

                    var wrapModeHash = HashUtility.CalculateHash(mainTexture.wrapMode);
                    HashUtility.AppendHash(wrapModeHash, ref hash);

                    if (uvMappingSettings.MappingMode == UvMappingSettings.Mode.ObjectSpace)
                    {
                        ObjectSpaceUvMap(uvMappingSettings, origSharedMesh, subMeshIndex, modifiedVertices, uvs, completeBounds);
                    }
                    else if (uvMappingSettings.MappingMode == UvMappingSettings.Mode.WorldSpace)
                    {
                        WorldSpaceUvMap(uvMappingSettings, origSharedMesh, subMeshIndex, meshRenderer.transform.localToWorldMatrix, modifiedVertices, uvs, completeBounds);
                    }
                    else
                    {
                        BasicUvMap(uvMappingSettings, subMesh, uvs, size);
                    }
                }

                modifiedMesh.SetUVs(uvChannel, uvs);
            }

            if (!hash.isValid)
            {
                MeshUtility.ResetUV(origSharedMesh, modifiedMesh);
            }

            return hash;
        }

        private static void BasicUvMap(UvMappingSettings uvMappingSettings, SubMeshDescriptor subMesh, List<Vector2> uvs, Vector3 size)
        {
            var firstVertex = subMesh.firstVertex;
            var vertexCount = subMesh.vertexCount;
            var lastVertex = firstVertex + vertexCount;

            for (int uvIndex = firstVertex; uvIndex < lastVertex; uvIndex++)
            {
                var uv = uvs[uvIndex];
                uv = Vector2.Scale(uv, uvMappingSettings.UvScale);
                uv += (Vector2)uvMappingSettings.SurfaceTextureOffset;

                var scaledUv = Vector2.zero;

                foreach (var mapping in uvMappingSettings.Mappings)
                {
                    var scaledVector = Vector3.Scale(size, mapping.SurfaceNormal);
                    var scaledVectorSum = scaledVector.x + scaledVector.y + scaledVector.z;

                    scaledUv += Vector2.Scale(uv, mapping.SurfaceTextureDirection) * scaledVectorSum;
                }

                uvs[uvIndex] = scaledUv;
            }
        }

        private static void WorldSpaceUvMap(UvMappingSettings uvMappingSettings, Mesh origSharedMesh, int subMeshIndex,
            Matrix4x4 localToWorldMatrix,
            List<Vector3> modifiedVertices, List<Vector2> uvs, Bounds completeBounds)
        {
            var triangles = TempCollections.Integers;

            var meshScale = completeBounds.size;
            meshScale = new Vector3(1 / meshScale.x, 1 / meshScale.y, 1 / meshScale.z);

            origSharedMesh.GetTriangles(triangles, subMeshIndex, true);

            for (int index = 0; index < triangles.Count; index += 3)
            {
                int i0 = triangles[index];
                int i1 = triangles[index + 1];
                int i2 = triangles[index + 2];

                Vector3 v0 = localToWorldMatrix.MultiplyPoint3x4(modifiedVertices[i0]);
                Vector3 v1 = localToWorldMatrix.MultiplyPoint3x4(modifiedVertices[i1]);
                Vector3 v2 = localToWorldMatrix.MultiplyPoint3x4(modifiedVertices[i2]);

                var (uv0, uv1, uv2) = uvMappingSettings.MapUvToTriangle(v0, v1, v2, meshScale);

                uvs[i0] = uv0;
                uvs[i1] = uv1;
                uvs[i2] = uv2;
            }
        }

        private static void ObjectSpaceUvMap(UvMappingSettings uvMappingSettings, Mesh origSharedMesh, int subMeshIndex,
            List<Vector3> modifiedVertices, List<Vector2> uvs, Bounds completeBounds)
        {
            var triangles = TempCollections.Integers;

            var meshScale = completeBounds.size;
            meshScale = new Vector3(1 / meshScale.x, 1 / meshScale.y, 1 / meshScale.z);

            origSharedMesh.GetTriangles(triangles, subMeshIndex, true);

            for (int index = 0; index < triangles.Count; index += 3)
            {
                int i0 = triangles[index];
                int i1 = triangles[index + 1];
                int i2 = triangles[index + 2];

                Vector3 v0 = modifiedVertices[i0];
                Vector3 v1 = modifiedVertices[i1];
                Vector3 v2 = modifiedVertices[i2];

                var (uv0, uv1, uv2) = uvMappingSettings.MapUvToTriangle(v0, v1, v2, meshScale);

                uvs[i0] = uv0;
                uvs[i1] = uv1;
                uvs[i2] = uv2;
            }
        }

        public static bool ValidSliceTarget(Texture texture, SubMeshDescriptor subMesh)
        {
            if (texture == null ||
                texture.wrapMode == TextureWrapMode.Clamp ||
                texture.wrapMode == TextureWrapMode.MirrorOnce ||
                texture.wrapModeU != texture.wrapModeV)
            {
                return false;
            }

            if (subMesh.topology != MeshTopology.Triangles)
            {
                // This only works with a triangle mesh topology
                return false;
            }

            return true;
        }
    }

    [Serializable]
    public struct UvMapping
    {
        public UvMapping(Vector3 surfaceNormal, Vector3 surfaceTextureDir)
        {
            this.SurfaceNormal = surfaceNormal;
            this.SurfaceTextureDirection = surfaceTextureDir;
        }

        public Vector3 SurfaceNormal;
        public Vector3 SurfaceTextureDirection;

        public UvMapping Inverse => new UvMapping(-SurfaceNormal, -SurfaceTextureDirection);

        public float CalculateFitScore(Plane trianglePlane)
        {
            return Vector3.Dot(trianglePlane.normal, SurfaceNormal);
        }

        public Hash128 CalculateHash()
        {
            var hash = HashUtility.CalculateHash(SurfaceTextureDirection);
            var normHash = HashUtility.CalculateHash(SurfaceNormal);

            HashUtility.AppendHash(normHash, ref hash);

            return hash;
        }
    }

    [Serializable]
    public class UvMappingSettings
    {
        public UvMappingSettings()
        {
            Reset();
        }

        public UvMappingSettings(List<UvMapping> mappings, bool generateInverseMappings, Vector2 uvScale, Vector3 surfaceTextureOffset)
        {
            Mappings = mappings;
            GenerateInverseMappings = generateInverseMappings;
            UvScale = uvScale;
            SurfaceTextureOffset = surfaceTextureOffset;
        }

        public Vector2 UvScale;
        public Vector3 SurfaceTextureOffset;
        public Mode MappingMode;
        public bool GenerateInverseMappings;
        public List<UvMapping> Mappings = new List<UvMapping>();

        public void Reset()
        {
            Mappings.Clear();

            if (MappingMode == Mode.UvSpace)
            {
                Mappings.Add(new UvMapping(Vector3.right, Vector3.right));
                Mappings.Add(new UvMapping(Vector3.forward, Vector3.up));
            }
            else
            {
                Mappings.Add(new UvMapping(Vector3.up, Vector3.forward));
                Mappings.Add(new UvMapping(Vector3.forward, Vector3.right));
                Mappings.Add(new UvMapping(Vector3.right, Vector3.back));

                GenerateInverseMappings = true;
            }

            UvScale = Vector2.one;
            SurfaceTextureOffset = Vector3.zero;
        }

        public void ResetMappings()
        {
            Mappings.Clear();

            if (MappingMode == Mode.UvSpace)
            {
                Mappings.Add(new UvMapping(Vector3.right, Vector3.right));
                Mappings.Add(new UvMapping(Vector3.forward, Vector3.up));
            }
            else
            {
                Mappings.Add(new UvMapping(Vector3.up, Vector3.forward));
                Mappings.Add(new UvMapping(Vector3.forward, Vector3.right));
                Mappings.Add(new UvMapping(Vector3.right, Vector3.back));

                GenerateInverseMappings = true;
            }
        }

        public bool AreMappingsDefault()
        {
            if (MappingMode == Mode.UvSpace)
            {
                if (Mappings.Count == 2 && 
                    Mappings.Contains(new UvMapping(Vector3.right, Vector3.right)) && 
                    Mappings.Contains(new UvMapping(Vector3.forward, Vector3.up)))
                {
                    return true;
                }
            }
            else
            {
                if (Mappings.Count == 3 && 
                    Mappings.Contains(new UvMapping(Vector3.up, Vector3.forward)) && 
                    Mappings.Contains(new UvMapping(Vector3.forward, Vector3.right)) && 
                    Mappings.Contains(new UvMapping(Vector3.right, Vector3.back)))
                {
                    return true;
                }
            }

            return false;
        }

        public (Vector2 uv0, Vector2 uv1, Vector2 uv2) MapUvToTriangle(Vector3 vertex0, Vector3 vertex1, Vector3 vertex2, Vector3 meshScale)
        {
            vertex0 += SurfaceTextureOffset;
            vertex1 += SurfaceTextureOffset;
            vertex2 += SurfaceTextureOffset;

            var plane = new Plane(vertex0, vertex1, vertex2);

            var bestUvMapping = GetBestFitUvMapping(plane);

            var textureYDirection = Vector3.Cross(plane.normal, bestUvMapping.SurfaceTextureDirection).normalized;
            var textureUDirection = Vector3.Cross(plane.normal, textureYDirection).normalized;
            var originProjectedOntoPlane = plane.ClosestPointOnPlane(Vector3.zero);

            var uv0 = MapUv(vertex0, textureUDirection, textureYDirection, originProjectedOntoPlane, meshScale);
            var uv1 = MapUv(vertex1, textureUDirection, textureYDirection, originProjectedOntoPlane, meshScale);
            var uv2 = MapUv(vertex2, textureUDirection, textureYDirection, originProjectedOntoPlane, meshScale);

            return (uv0, uv1, uv2);
        }

        private Vector2 MapUv(Vector3 vertex, Vector3 textureUDirection, Vector3 textureYDirection, Vector3 originProjectedToPlane, Vector3 meshScale)
        {
            var nv = (Vector3.Scale(vertex, meshScale)) - originProjectedToPlane;
            var u = Vector3.Dot(nv, textureUDirection) * UvScale.x;
            var v = Vector3.Dot(nv, textureYDirection) * UvScale.y;

            return new Vector2(u, v);
        }

        public UvMapping GetBestFitUvMapping(Plane trianglePlane)
        {
            var bestFitScore = -1f;
            UvMapping? bestFit = null;

            foreach (var mapping in Mappings)
            {
                var fitScore = mapping.CalculateFitScore(trianglePlane);
                if (fitScore > bestFitScore)
                {
                    bestFitScore = fitScore;
                    bestFit = mapping;
                }

                if (GenerateInverseMappings)
                {
                    var invertedFitScore = fitScore * -1;
                    if (invertedFitScore > bestFitScore)
                    {
                        bestFitScore = invertedFitScore;
                        bestFit = mapping.Inverse;
                    }
                }
            }

            if (!bestFit.HasValue)
            {
                return new UvMapping(Vector3.forward, Vector3.right);
            }

            return bestFit.Value;
        }

        public Hash128 CalculateHash()
        {
            var hash = HashUtility.CalculateHash(UvScale);

            var inverseHash = HashUtility.CalculateHash(GenerateInverseMappings, 6);
            var offsetHash = HashUtility.CalculateHash(SurfaceTextureOffset);
            var modeHash = HashUtility.CalculateHash(MappingMode);
            HashUtility.AppendHash(modeHash, inverseHash, offsetHash, ref hash);

            foreach (var mapping in Mappings)
            {
                var mappingHash = mapping.CalculateHash();
                HashUtility.AppendHash(mappingHash, ref hash);
            }

            return hash;
        }

        public enum Mode
        {
            UvSpace = 0,
            ObjectSpace = 1,
            WorldSpace = 2,
        }
    }
}

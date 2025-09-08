// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using System.Collections.Generic;
using UnityEngine;

namespace Slicer.Core
{
    /// <summary>
    /// Contains useful methods for working with <c>UnityEngine.Bounds</c>.
    /// </summary>
    public static class BoundsUtility
    {
        /// <summary>
        /// Grow the bounds to encapsulate the bounds.
        /// </summary>
        /// <param name="boundA">the first bounds to encapsulate.</param>
        /// <param name="boundB">the second bounds to encapsulate.</param>
        /// <returns>
        /// Returns the bounds that encapsulates both bounds.
        /// 
        /// If <c>boundB</c> is null, <c>boundA</c> is returned.
        /// </returns>
        public static Bounds Encapsulate(Bounds boundA, Bounds? boundB)
        {
            return Encapsulate(boundB, boundA);
        }

        /// <summary>
        /// Grow the bounds to encapsulate the bounds.
        /// </summary>
        /// <param name="boundA">the first bounds to encapsulate.</param>
        /// <param name="boundB">the second bounds to encapsulate.</param>
        /// <returns>
        /// Returns the bounds that encapsulates both bounds.
        /// 
        /// If <c>boundA</c> is null, <c>boundB</c> is returned.
        /// </returns>
        public static Bounds Encapsulate(Bounds? boundA, Bounds boundB)
        {
            if (!boundA.HasValue)
            {
                return boundB;
            }

            boundB.Encapsulate(boundA.Value);

            return boundB;
        }

        /// <summary>
        /// Grow the bounds to encapsulate the bounds.
        /// </summary>
        /// <param name="boundA">the first bounds to encapsulate.</param>
        /// <param name="boundB">the second bounds to encapsulate.</param>
        /// <returns>
        /// Returns the bounds that encapsulates both bounds.
        /// 
        /// If one of the supplied bounds is null, the other supplied bounds is returned.
        /// 
        /// If both bounds are null, null is returned. 
        /// </returns>
        public static Bounds? Encapsulate(Bounds? boundA, Bounds? boundB)
        {
            if (!boundA.HasValue && !boundB.HasValue)
            {
                return null;
            }

            if (!boundA.HasValue)
            {
                return boundB;
            }

            if (!boundB.HasValue)
            {
                return boundA;
            }

            var a = boundA.Value;
            var b = boundB.Value;

            a.Encapsulate(b);

            return a;
        }

        /// <summary>
        /// Converts a bounds into a collection of verts, one for each corner
        /// </summary>
        public static void GetVerts(Bounds bounds, List<Vector3> vectors)
        {
            vectors.Clear();

            Vector3 min = bounds.min;
            Vector3 s = bounds.size;
            Vector3 vert;

            vert = new Vector3(min.x, min.y, min.z);
            vectors.Add(vert);

            vert = new Vector3(min.x + s.x, min.y, min.z);
            vectors.Add(vert);

            vert = new Vector3(min.x, min.y, min.z + s.z);
            vectors.Add(vert);

            vert = new Vector3(min.x + s.x, min.y, min.z + s.z);
            vectors.Add(vert);

            vert = new Vector3(min.x, min.y + s.y, min.z);
            vectors.Add(vert);

            vert = new Vector3(min.x + s.x, min.y + s.y, min.z);
            vectors.Add(vert);

            vert = new Vector3(min.x, min.y + s.y, min.z + s.z);
            vectors.Add(vert);

            vert = new Vector3(min.x + s.x, min.y + s.y, min.z + s.z);
            vectors.Add(vert);
        }

        /// <summary>
        /// Encapsulates a collection of vectors into a bounds.
        /// </summary>
        /// <param name="vectors">The vectors to encapsulate.</param>
        /// <returns>Returns the bounds that contains the supplied vectors.</returns>
        public static Bounds Encapsulate(List<Vector3> vectors)
        {
            var min = vectors[0];
            var max = min;

            // Iterate through all verts except first one
            for (var i = 1; i < vectors.Count; i++)
            {
                var v = vectors[i];

                max = Vector3.Max(v, max);
                min = Vector3.Min(v, min);
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);

            return bounds;
        }

        /// <summary>
        /// Converts the properties of a <c>BoxCollider</c> into a bounds, in the objects local space.
        /// </summary>
        /// <param name="boxCollider">The box collider to convert</param>
        /// <returns>Returns the bounds of the box collider</returns>
        public static Bounds AsBounds(BoxCollider boxCollider)
        {
            var bounds = new Bounds(boxCollider.center, boxCollider.size);

            return bounds;
        }

        /// <summary>
        /// Converts a local bounds into a bounds in slicer space (local space of the parent SlicerComponent).
        /// </summary>
        /// <param name="bounds">The bounds to encapsulate into the returned bounds.</param>
        /// <param name="childTransform">The transform of the GameObject that contains the mesh.</param>
        /// <param name="rootTransform">The transform of the GameObject containing the <c>SlicerController</c>.</param>
        /// <returns>The bounds that contains the bounds transformed extents.</returns>
        public static Bounds CalculateBounds(Bounds bounds, Transform childTransform, Transform rootTransform)
        {
            var matrix = MatrixUtility.BuildTransformMatrix(childTransform, rootTransform);

            GetVerts(bounds, TempCollections.Vector3);

            var transformedBounds = VectorUtility.CalculateBounds(TempCollections.Vector3, matrix);

            TempCollections.Vector3.Clear();
            return transformedBounds;
        }
    }
}

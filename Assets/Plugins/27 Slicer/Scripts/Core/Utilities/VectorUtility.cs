// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using System.Collections.Generic;
using UnityEngine;

namespace Slicer.Core
{
    /// <summary>
    /// Contains useful methods for working with <c>UnityEngine.Vector3</c>.
    /// </summary>
    public static class VectorUtility
    {
        /// <summary>
        /// Transforms the supplied vector by the supplied matrix
        /// </summary>
        /// <param name="v">The vector to transform</param>
        /// <param name="matrix">The matrix to multiply by</param>
        /// <returns>Returns the vectors transformed by the matrix</returns>
        public static Vector3 TransformVector(Vector3 v, Matrix4x4 matrix)
        {
            v = matrix.MultiplyPoint3x4(v);
            return v;
        }

        /// <summary>
        /// Calculates the bounds that encapsulates all of the vectors after they have been transformed by the supplied matrix.
        /// </summary>
        /// <param name="vectors">The vectors to encapsulate into the returned bounds.</param>
        /// <param name="matrix">The matrix to transform all of the vectors by.</param>
        /// <returns>The bounds that contains all of the transformed vectors.</returns>
        public static Bounds CalculateBounds(List<Vector3> vectors, Matrix4x4 matrix)
        {
            var min = TransformVector(vectors[0], matrix);
            var max = min;

            // Iterate through all verts except first one
            for (var i = 1; i < vectors.Count; i++)
            {
                var v = TransformVector(vectors[i], matrix);

                max = Vector3.Max(v, max);
                min = Vector3.Min(v, min);
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);

            return bounds;
        }
    }
}

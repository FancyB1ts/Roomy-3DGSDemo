// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEngine;

namespace Slicer.Core
{
    /// <summary>
    /// Contains useful methods for working with <c>UnityEngine.Matrix4x4</c>.
    /// </summary>
    public static class MatrixUtility
    {
        /// <summary>
        /// Builds a transform matrix that transforms <c>childTransform</c> into the Local Object Space of <c>rootTransform</c>.
        /// </summary>
        /// <param name="childTransform">The transform used to build the Matrix</param>
        /// <param name="rootTransform">The transform to use as the Local Object Space</param>
        /// <returns>Returns a transform matrix in Local Object Space of the GameObject the rootTransform.</returns>
        public static Matrix4x4 BuildTransformMatrix(Transform childTransform, Transform rootTransform)
        {
            var modelMatrix = childTransform.localToWorldMatrix;
            var inversedRootMatrix = rootTransform.worldToLocalMatrix;
            var matrix = inversedRootMatrix * modelMatrix;
            return matrix;
        }

        /// <summary>
        /// Builds an inverse transform matrix that transforms <c>childTransform</c> into the Local Object Space of <c>rootTransform</c>.
        /// </summary>
        /// <param name="childTransform">The transform used to build the Matrix</param>
        /// <param name="rootTransform">The transform to use as the Local Object Space</param>
        /// <returns>Returns a transform matrix in Local Object Space of the GameObject the rootTransform.</returns>
        public static Matrix4x4 BuildInverseTransformMatrix(Transform childTransform, Transform rootTransform)
        {
            var matrix = BuildTransformMatrix(rootTransform, childTransform);
            return matrix;
        }
    }
}

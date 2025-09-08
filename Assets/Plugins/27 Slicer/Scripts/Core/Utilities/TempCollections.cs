// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Slicer.Core
{
    /// <summary>
    /// Contains collections that can be used with non-allocating methods like <see cref="Mesh.GetVertices(List{Vector3})"/>.
    /// </summary>
    public static class TempCollections
    {
        public static readonly List<Material> Materials = new List<Material>();

        public static readonly List<Collider> Colliders = new List<Collider>();

        public static readonly List<int> Integers = new List<int>();

        public static readonly List<Color> Colors = new List<Color>();

        public static readonly List<Vector4> Vector4 = new List<Vector4>();

        public static readonly List<Vector3> Vector3 = new List<Vector3>();
        public static readonly List<Vector3> Vector3_2 = new List<Vector3>();

        public static readonly List<Vector2> Vector2 = new List<Vector2>();
        public static readonly List<Vector2> Vector2_2 = new List<Vector2>();

        public static void Clear()
        {
            Materials.Clear();
            Materials.TrimExcess();

            Colliders.Clear();
            Colliders.TrimExcess();

            Integers.Clear();
            Integers.TrimExcess();

            Colors.Clear();
            Colors.TrimExcess();

            Vector4.Clear();
            Vector4.TrimExcess();

            Vector3.Clear();
            Vector3.TrimExcess();

            Vector3_2.Clear();
            Vector3_2.TrimExcess();

            Vector2.Clear();
            Vector2.TrimExcess();

            Vector2_2.Clear();
            Vector2_2.TrimExcess();
        }
    }
}

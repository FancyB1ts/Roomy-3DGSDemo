// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using System;
using UnityEngine;

namespace Slicer.Core
{
    /// <summary>
    /// Contains useful methods for working with <c>UnityEngine.Hash128</c>.
    /// </summary>
    public static class HashUtility
    {
        /// <summary>
        /// Calculates the hash of an <c>UnityEngine.Object</c>.
        /// </summary>
        /// <remarks>
        /// <c>UnityEngine.Object.GetInstanceID()</c> is used to calculate the hash.
        /// </remarks>
        /// <param name="o">The object to calculate the hash of.</param>
        /// <returns>The calculated hash.</returns>
        public static Hash128 CalculateHash(UnityEngine.Object o)
        {
            var i = o.GetInstanceID();
            var h = CalculateHash(i);
            return h;
        }

        /// <summary>
        /// Calculates the hash of a <c>struct</c>.
        /// </summary>
        /// <typeparam name="T">the type of object to hash, must be of type <c>struct</c>.</typeparam>
        /// <param name="s">The <c>struct</c> to hash.</param>
        /// <returns>The calculated hash.</returns>
        public static Hash128 CalculateHash<T>(T s) where T : struct
        {
            Hash128 h = new Hash128();
            HashUtilities.ComputeHash128(ref s, ref h);
            return h;
        }

        /// <summary>
        /// Calculates the hash of a <c>bool</c>.
        /// </summary>
        /// <param name="b">The <c>bool</c> to hash.</param>
        /// <param name="shift">How many bits to shift the bool by after converting it to a <c>UInt32</c></param>
        /// <returns>The calculated hash.</returns>
        public static Hash128 CalculateHash(bool b, int shift)
        {
            var i = Convert.ToUInt32(b);
            i = i << shift;

            var h = CalculateHash(i);
            return h;
        }

        /// <summary>
        /// Calculates the hash of a <c>Vector3</c>.
        /// </summary>
        /// <param name="v">The <c>Vector3</c> to hash.</param>
        /// <returns>The calculated hash.</returns>
        public static Hash128 CalculateHash(Vector3 v)
        {
            Hash128 h = new Hash128();
            HashUtilities.QuantisedVectorHash(ref v, ref h);
            return h;
        }

        /// <summary>
        /// Calculates the hash of a <c>Matrix4x4</c>.
        /// </summary>
        /// <param name="m">The <c>Matrix4x4</c> to hash.</param>
        /// <returns>The calculated hash.</returns>
        public static Hash128 CalculateHash(Matrix4x4 m)
        {
            Hash128 h = new Hash128();
            HashUtilities.QuantisedMatrixHash(ref m, ref h);
            return h;
        }

        /// <summary>
        /// Calculates the hash of a <c>Bounds</c>.
        /// </summary>
        /// <param name="b">The <c>Bounds</c> to hash.</param>
        /// <returns>The calculated hash.</returns>
        public static Hash128 CalculateHash(Bounds b)
        {
            var c = CalculateHash(b.center);
            var e = CalculateHash(b.extents);
            AppendHash(c, ref e);

            return e;
        }

        /// <summary>
        /// Appends two hashes.
        /// </summary>
        /// <param name="inHash">Hash to append with.</param>
        /// <param name="outHash">The hash that will be updated.</param>
        public static void AppendHash(Hash128 inHash, ref Hash128 outHash)
        {
            HashUtilities.AppendHash(ref inHash, ref outHash);
        }

        /// <summary>
        /// Appends three hashes.
        /// </summary>
        /// <param name="inHash1">Hash 1 to append with.</param>
        /// <param name="inHash2">Hash 2 to append with.</param>
        /// <param name="outHash">The hash that will be updated.</param>
        public static void AppendHash(Hash128 inHash1, Hash128 inHash2, ref Hash128 outHash)
        {
            HashUtilities.AppendHash(ref inHash1, ref inHash2);
            HashUtilities.AppendHash(ref inHash2, ref outHash);
        }

        /// <summary>
        /// Appends four hashes.
        /// </summary>
        /// <param name="inHash1">Hash 1 to append with.</param>
        /// <param name="inHash2">Hash 2 to append with.</param>
        /// <param name="inHash3">Hash 3 to append with.</param>
        /// <param name="outHash">The hash that will be updated.</param>
        public static void AppendHash(Hash128 inHash1, Hash128 inHash2, Hash128 inHash3, ref Hash128 outHash)
        {
            HashUtilities.AppendHash(ref inHash1, ref inHash2);
            HashUtilities.AppendHash(ref inHash2, ref inHash3);
            HashUtilities.AppendHash(ref inHash3, ref outHash);
        }

        /// <summary>
        /// Appends five hashes.
        /// </summary>
        /// <param name="inHash1">Hash 1 to append with.</param>
        /// <param name="inHash2">Hash 2 to append with.</param>
        /// <param name="inHash3">Hash 3 to append with.</param>
        /// <param name="inHash4">Hash 4 to append with.</param>
        /// <param name="outHash">The hash that will be updated.</param>
        public static void AppendHash(Hash128 inHash1, Hash128 inHash2, Hash128 inHash3, Hash128 inHash4, ref Hash128 outHash)
        {
            HashUtilities.AppendHash(ref inHash1, ref inHash2);
            HashUtilities.AppendHash(ref inHash2, ref inHash3);
            HashUtilities.AppendHash(ref inHash3, ref inHash4);
            HashUtilities.AppendHash(ref inHash4, ref outHash);
        }

        /// <summary>
        /// Appends six hashes.
        /// </summary>
        /// <param name="inHash1">Hash 1 to append with.</param>
        /// <param name="inHash2">Hash 2 to append with.</param>
        /// <param name="inHash3">Hash 3 to append with.</param>
        /// <param name="inHash4">Hash 4 to append with.</param>
        /// <param name="inHash5">Hash 5 to append with.</param>
        /// <param name="outHash">The hash that will be updated.</param>
        public static void AppendHash(Hash128 inHash1, Hash128 inHash2, Hash128 inHash3, Hash128 inHash4, Hash128 inHash5, ref Hash128 outHash)
        {
            HashUtilities.AppendHash(ref inHash1, ref inHash2);
            HashUtilities.AppendHash(ref inHash2, ref inHash3);
            HashUtilities.AppendHash(ref inHash3, ref inHash4);
            HashUtilities.AppendHash(ref inHash4, ref inHash5);
            HashUtilities.AppendHash(ref inHash5, ref outHash);
        }

        /// <summary>
        /// Appends seven hashes.
        /// </summary>
        /// <param name="inHash1">Hash 1 to append with.</param>
        /// <param name="inHash2">Hash 2 to append with.</param>
        /// <param name="inHash3">Hash 3 to append with.</param>
        /// <param name="inHash4">Hash 4 to append with.</param>
        /// <param name="inHash5">Hash 5 to append with.</param>
        /// <param name="inHash6">Hash 6 to append with.</param>
        /// <param name="outHash">The hash that will be updated.</param>
        public static void AppendHash(Hash128 inHash1, Hash128 inHash2, Hash128 inHash3, Hash128 inHash4, Hash128 inHash5, Hash128 inHash6, ref Hash128 outHash)
        {
            HashUtilities.AppendHash(ref inHash1, ref inHash2);
            HashUtilities.AppendHash(ref inHash2, ref inHash3);
            HashUtilities.AppendHash(ref inHash3, ref inHash4);
            HashUtilities.AppendHash(ref inHash4, ref inHash5);
            HashUtilities.AppendHash(ref inHash5, ref inHash6);
            HashUtilities.AppendHash(ref inHash6, ref outHash);
        }
    }
}

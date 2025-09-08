// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using Slicer.Core;
using System;

namespace Slicer.Editor
{
    internal static class Helpers
    {
        private static HashSet<object> ActiveFoldouts = new HashSet<object>();
        
        public static bool IsTrackedFoldout(object o)
        {
            return ActiveFoldouts.Contains(o.GetHashCode());
        }

        public static void AddFoldoutTracking(object o)
        {
            ActiveFoldouts.Add(o.GetHashCode());
        }

        public static void RemoveFoldoutTracking(object o)
        {
            ActiveFoldouts.Remove(o.GetHashCode());
        }

        public static T GetSerializedValue<T>(this SerializedProperty property)
        {
            object obj = property.serializedObject.targetObject;
            string[] propertyNames = property.propertyPath.Split('.');

            // Clear the property path from "Array" and "data[i]".
            if (propertyNames.Length >= 3 && propertyNames[propertyNames.Length - 2] == "Array")
            {
                propertyNames = propertyNames.Take(propertyNames.Length - 2).ToArray();
            }

            // Get the last object of the property path.
            foreach (string path in propertyNames)
            {
                obj = obj.GetType()
                    .GetField(path, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    .GetValue(obj);
            }

            if (obj.GetType().GetInterfaces().Contains(typeof(IList<T>)))
            {
                int propertyIndex = int.Parse(property.propertyPath[property.propertyPath.Length - 2].ToString());

                return ((IList<T>)obj)[propertyIndex];
            }
            else return (T)obj;
        }

        /// <summary>
        /// Compares two paths and returns if they are the same path.
        /// </summary>
        /// <param name="path1">The first path to compare</param>
        /// <param name="path2">The second path to compare</param>
        /// <returns>Returns true if the paths are the same</returns>
        public static bool ComparePaths(string path1, string path2)
        {
            var fullPath1 = Path.GetFullPath(path1).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullPath2 = Path.GetFullPath(path2).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var result = string.Compare(fullPath1, fullPath2,
                StringComparison.InvariantCultureIgnoreCase);

            return result == 0;
        }
    }
}

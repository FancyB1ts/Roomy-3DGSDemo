using UnityEngine;
using UnityEditor;
using System;
using Slicer.Core;

namespace Slicer.Editor
{
    [CustomPropertyDrawer(typeof(UvMappingSettings))]
    public class UvMappingSettingsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var iterator = property.Copy(); // Create a copy of the SerializedProperty to iterate through its children.
                bool enterChildren = true;

                var mappingModeProperty = property.FindPropertyRelative(nameof(UvMappingSettings.MappingMode));
                var mappingMode = (UvMappingSettings.Mode)mappingModeProperty.enumValueIndex;

                while (iterator.NextVisible(enterChildren)) // Loop through all children
                {
                    if (SerializedProperty.EqualContents(iterator, property.GetEndProperty())) // Avoid drawing the parent property itself
                    {
                        break;
                    }

                    // hide GenerateInverseMappings when MappingMode is UvSpace
                    if (mappingMode == UvMappingSettings.Mode.UvSpace)
                    {
                        if (iterator.name == nameof(UvMappingSettings.GenerateInverseMappings))
                        {
                            continue; // Skip rendering GenerateInverseMappings
                        }
                    }

                    // Calculate position for the next property.
                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    position.height = EditorGUI.GetPropertyHeight(iterator, true);

                    EditorGUI.PropertyField(position, iterator, true); // Draw property

                    enterChildren = false;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight; // If not expanded, just return the single line height.
            }

            float totalHeight = EditorGUIUtility.singleLineHeight; // Start with one line for the foldout.

            var iterator = property.Copy(); // Create a copy of the SerializedProperty to iterate through its children.
            bool enterChildren = true;

            var mappingModeProperty = property.FindPropertyRelative(nameof(UvMappingSettings.MappingMode));
            var mappingMode = (UvMappingSettings.Mode)mappingModeProperty.enumValueIndex;

            while (iterator.NextVisible(enterChildren)) // Loop through all children
            {
                if (SerializedProperty.EqualContents(iterator, property.GetEndProperty())) // Avoid processing the parent property itself
                {
                    break;
                }

                if (mappingMode == UvMappingSettings.Mode.UvSpace)
                {
                    if (iterator.name == nameof(UvMappingSettings.GenerateInverseMappings))
                    {
                        continue; // Skip rendering GenerateInverseMappings
                    }
                }

                enterChildren = false;

                totalHeight += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing; // Add height of each property.
            }

            return totalHeight;
        }
    }

}
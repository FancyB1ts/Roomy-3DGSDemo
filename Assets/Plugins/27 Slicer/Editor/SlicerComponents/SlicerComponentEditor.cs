// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEditor;

namespace Slicer.Editor
{
    [CustomEditor(typeof(SlicerComponent))]
    [CanEditMultipleObjects]
    public abstract class SlicerComponentEditor : UnityEditor.Editor
    {
        protected SerializedProperty scriptProperty;

        protected SerializedProperty skipBoundsCalculationProperty;

        protected virtual void OnEnable()
        {
            scriptProperty = serializedObject.FindProperty("m_Script");
            skipBoundsCalculationProperty = serializedObject.FindProperty(nameof(SlicerComponent.SkipBoundsCalculation));
        }

        protected virtual void ShowDetails(SlicerComponent target)
        {
            EditorGUILayout.LabelField($"Enabled: {target.SlicingEnabled}");
        }

        protected virtual void HelpBox()
        {
            if (skipBoundsCalculationProperty.boolValue)
            {
                EditorGUILayout.HelpBox($"This component will not be included in Bounds Calculations.\nUntick '{skipBoundsCalculationProperty.displayName}' to include in calculations.", MessageType.Info);
            }
        }
    }
}
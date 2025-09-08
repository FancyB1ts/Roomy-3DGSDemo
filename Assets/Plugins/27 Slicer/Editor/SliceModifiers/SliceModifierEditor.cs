// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEditor;

namespace Slicer.Editor
{
    [CustomEditor(typeof(SliceModifier))]
    [CanEditMultipleObjects]
    public abstract class SliceModifierEditor : UnityEditor.Editor
    {
        protected SerializedProperty scriptProperty;

        protected virtual void OnEnable()
        {
            scriptProperty = serializedObject.FindProperty("m_Script");
        }

        protected virtual void ShowDetails(SliceModifier sliceModifier)
        {
            EditorGUILayout.LabelField($"Enabled: {sliceModifier.ModifierEnabled}");
        }

        protected virtual void HelpBox()
        {

        }
    }
}
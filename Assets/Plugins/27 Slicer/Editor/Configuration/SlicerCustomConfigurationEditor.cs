// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using UnityEditor;
using UnityEngine;

namespace Slicer.Editor
{
    [CustomEditor(typeof(SlicerCustomConfiguration))]
    class SlicerCustomConfigurationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // We want the Inspector to be read only if an asset is selected.
            GUI.enabled = false;
            DrawDefaultInspector();
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("This configuration is read only.");
            EditorGUILayout.LabelField("Navigate to Window -> 27 Slicer -> Configuration to edit it.");
        }
    }
}
// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEditor;
using UnityEngine;

namespace Slicer.Editor
{
    [CustomEditor(typeof(ScaleSliceModifier))]
    [CanEditMultipleObjects]
    public class ScaleSliceModifierEditor : SliceModifierEditor
    {
        private SerializedProperty originalScaleProperty;
        private SerializedProperty slicedScaleProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            originalScaleProperty = serializedObject.FindProperty("originalScale");
            slicedScaleProperty = serializedObject.FindProperty("slicedScale");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var originalScale = originalScaleProperty.vector3Value;
            var guiContent = new GUIContent(originalScaleProperty.displayName, "The scale of the Game Object before slicing.");
            var newOriginalScale = EditorGUILayout.Vector3Field(guiContent, originalScale);
            if (newOriginalScale != originalScale)
            {
                var scaleSliceModifier = (ScaleSliceModifier)target;
                scaleSliceModifier.SetUnslicedScale(newOriginalScale);

                var slicerController = scaleSliceModifier.GetParentSlicerController();
                slicerController.RefreshSliceImmediate();
            }

            DetailsBox();

            HelpBox();
        }

        public void DetailsBox()
        {
            EditorGUILayout.Space();

            scriptProperty.isExpanded = EditorGUILayout.Foldout(scriptProperty.isExpanded, "Slicing Details");
            if (scriptProperty.isExpanded)
            {
                if (targets.Length > 1)
                {
                    foreach (var target in targets)
                    {
                        EditorGUI.indentLevel++;
                        var foldout = Helpers.IsTrackedFoldout(target);
                        foldout = EditorGUILayout.Foldout(foldout, target.name);
                        if (foldout)
                        {
                            ShowDetails((SliceModifier)target);
                            Helpers.AddFoldoutTracking(target);
                        }
                        else
                        {
                            Helpers.RemoveFoldoutTracking(target);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    ShowDetails((SliceModifier)target);
                }
            }
        }

        protected override void ShowDetails(SliceModifier sliceModifier)
        {
            var scaleSliceModifier = (ScaleSliceModifier)sliceModifier;
            EditorGUI.indentLevel++;

            base.ShowDetails(scaleSliceModifier);


            EditorGUILayout.LabelField($"Original Scale: {scaleSliceModifier.OriginalScale.ToString("0.00")}");
            EditorGUILayout.LabelField($"Sliced Scale: {scaleSliceModifier.SlicedScale.ToString("0.00")}");

            EditorGUI.indentLevel--;
        }

        protected override void HelpBox()
        {
            if (!Application.isPlaying)
            {
                var scaleSliceModifier = (ScaleSliceModifier)target;
                var currentScale = scaleSliceModifier.transform.localScale;
                var originalScale = originalScaleProperty.vector3Value;
                var slicedScale = slicedScaleProperty.vector3Value;
                if (currentScale != originalScale && currentScale != slicedScale)
                {
                    EditorGUILayout.HelpBox($"The scale must be set either by the '{originalScaleProperty.displayName}' property. Or via the Game Objects transform while the parent slicer controller is in edit mode.", MessageType.Info);
                }
            }

            base.HelpBox();
        }

        private void OnSceneGUI()
        {

        }
    }
}
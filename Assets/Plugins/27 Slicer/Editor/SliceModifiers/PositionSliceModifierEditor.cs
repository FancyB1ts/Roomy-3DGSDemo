// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEditor;
using UnityEngine;

namespace Slicer.Editor
{
    [CustomEditor(typeof(PositionSliceModifier))]
    [CanEditMultipleObjects]
    public class PositionSliceModifierEditor : SliceModifierEditor
    {
        private SerializedProperty originalPositionProperty;
        private SerializedProperty slicedPositionProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            originalPositionProperty = serializedObject.FindProperty("originalPosition");
            slicedPositionProperty = serializedObject.FindProperty("slicedPosition");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var guiContent = new GUIContent(originalPositionProperty.displayName, "The position of the Game Object before slicing.");
            EditorGUILayout.PropertyField(originalPositionProperty, guiContent);
            if (serializedObject.ApplyModifiedProperties())
            {
                var positionSliceModifier = (PositionSliceModifier)target;
                positionSliceModifier.SetUnslicedPosition(originalPositionProperty.vector3Value);

                var slicerController = positionSliceModifier.GetParentSlicerController();
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
            var positionSliceModifier = (PositionSliceModifier)sliceModifier;
            EditorGUI.indentLevel++;

            base.ShowDetails(positionSliceModifier);

            EditorGUILayout.LabelField($"Original Position: {positionSliceModifier.OriginalPosition.ToString("0.00")}");
            EditorGUILayout.LabelField($"Sliced Position: {positionSliceModifier.SlicedPosition.ToString("0.00")}");

            EditorGUI.indentLevel--;
        }

        protected override void HelpBox()
        {
            if (!Application.isPlaying)
            {
                var positionSliceModifier = (PositionSliceModifier)target;
                var currentPosition = positionSliceModifier.transform.localPosition;
                var originalPosition = originalPositionProperty.vector3Value;
                var slicedPosition = slicedPositionProperty.vector3Value;
                if (currentPosition != originalPosition && currentPosition != slicedPosition)
                {
                    EditorGUILayout.HelpBox($"The position must be set either by the '{originalPositionProperty.displayName}' property. Or via the Game Objects transform while the parent slicer controller is in edit mode.", MessageType.Info);
                }
            }

            base.HelpBox();
        }

        private void OnSceneGUI()
        {
            var modifier = (PositionSliceModifier)target;
            var transform = modifier.transform;
            using (new Handles.DrawingScope(Color.yellow, transform.localToWorldMatrix))
            {
                float size = HandleUtility.GetHandleSize(modifier.Anchor) * 0.125f;
                var fmh_115_75_638859420571451600 = transform.rotation; modifier.Anchor = Handles.FreeMoveHandle(modifier.Anchor, size, Vector3.zero, Handles.SphereHandleCap);
            }
        }
    }
}
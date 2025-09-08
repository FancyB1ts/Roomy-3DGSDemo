// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEditor;

namespace Slicer.Editor
{
    [CustomEditor(typeof(SlicerControllerSliceModifier))]
    [CanEditMultipleObjects]
    public class SliceControllerSliceModifierEditor : SliceModifierEditor
    {
        private SerializedProperty originalSizeProperty;
        private SerializedProperty slicedSizeProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            originalSizeProperty = serializedObject.FindProperty("originalSize");
            slicedSizeProperty = serializedObject.FindProperty("slicedSize");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

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
            var slicerControllerSliceModifier = (SlicerControllerSliceModifier)sliceModifier;
            EditorGUI.indentLevel++;

            base.ShowDetails(sliceModifier);

            EditorGUILayout.LabelField($"Original Size: {slicerControllerSliceModifier.OriginalSize.ToString("0.00")}");
            EditorGUILayout.LabelField($"Sliced Size: {slicerControllerSliceModifier.SlicedSize.ToString("0.00")}");

            EditorGUILayout.LabelField($"Original Offset: {slicerControllerSliceModifier.OriginalOffset.ToString("0.00")}");
            EditorGUILayout.LabelField($"Sliced Offset: {slicerControllerSliceModifier.SlicedOffset.ToString("0.00")}");

            EditorGUI.indentLevel--;
        }

        protected override void HelpBox()
        {
            base.HelpBox();
        }

        private void OnSceneGUI()
        {

        }
    }
}
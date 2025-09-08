// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using System;
using UnityEditor;
using UnityEngine;

namespace Slicer.Editor
{
    [CustomEditor(typeof(MeshSlicerComponent))]
    [CanEditMultipleObjects]
    public class MeshSlicerComponentEditor : SlicerComponentEditor
    {
        private SerializedProperty skipUvsProperty;
        private SerializedProperty uvMappingSettingsProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            skipUvsProperty = serializedObject.FindProperty(nameof(MeshSlicerComponent.SkipUvs));
            uvMappingSettingsProperty = serializedObject.FindProperty(nameof(MeshSlicerComponent.UvMappingSettings));
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();

            DrawPropertiesExcluding(serializedObject, nameof(MeshSlicerComponent.UvMappingSettings));

            UvMappingSettings();

            serializedObject.ApplyModifiedProperties();
            var changed = EditorGUI.EndChangeCheck();

            if (changed)
            {
                foreach (var t in targets)
                {
                    EditorUtility.SetDirty(t);
                }
            }

            DetailsBox();

            HelpBox();
        }

        public void UvMappingSettings()
        {
            if (skipUvsProperty.boolValue)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(uvMappingSettingsProperty, true);

            if (uvMappingSettingsProperty.isExpanded)
            {
                GUI.enabled = !AreMappingsDefault();

                var resetButtonContent = new GUIContent("\u21ba");
                var resetButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 15
                };
                var rect = GUILayoutUtility.GetRect(resetButtonContent, resetButtonStyle, GUILayout.ExpandWidth(false));
                rect.y += (EditorGUIUtility.singleLineHeight * 3) + (rect.height - EditorGUIUtility.singleLineHeight);
                if (GUI.Button(rect, resetButtonContent, resetButtonStyle))
                {
                    ResetUvMappings();
                }

                GUI.enabled = false;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ResetUvMappings()
        {
            foreach (var t in targets)
            {
                var meshSlicerComponent = (MeshSlicerComponent)t;

                meshSlicerComponent.UvMappingSettings.ResetMappings();
            }
        }

        private bool AreMappingsDefault()
        {
            foreach (var t in targets)
            {
                var meshSlicerComponent = (MeshSlicerComponent)t;

                var areDefault = meshSlicerComponent.UvMappingSettings.AreMappingsDefault();

                if (!areDefault)
                {
                    return false;
                }
            }

            return true;
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
                            ShowDetails((SlicerComponent)target);
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
                    ShowDetails((SlicerComponent)target);
                }
            }
        }

        protected override void ShowDetails(SlicerComponent slicerComponent)
        {
            var meshSlicerComponent = (MeshSlicerComponent)slicerComponent;
            EditorGUI.indentLevel++;

            base.ShowDetails(meshSlicerComponent);

            var meshDetailsList = meshSlicerComponent.MeshDetailsList;

            if (meshDetailsList == null)
            {
                return;
            }

            var managedMeshCount = meshDetailsList.Count;
            if (managedMeshCount == 0)
            {
                return;
            }

            EditorGUILayout.LabelField($"Managed Count: {managedMeshCount}");

            var foldoutTracker = (meshSlicerComponent, meshDetailsList.GetType());
            var foldout = Helpers.IsTrackedFoldout(foldoutTracker);
            foldout = EditorGUILayout.Foldout(foldout, "Mesh Details List");
            if (foldout)
            {
                for (int i = 0; i < managedMeshCount; i++)
                {
                    var meshDetailProperty = meshDetailsList[i];

                    ShowMeshDetails(meshDetailProperty, i);
                }
                Helpers.AddFoldoutTracking(foldoutTracker);
            }
            else
            {
                Helpers.RemoveFoldoutTracking(foldoutTracker);
            }

            EditorGUI.indentLevel--;
        }

        private void ShowMeshDetails(MeshDetails meshDetail, int index)
        {
            EditorGUI.indentLevel++;

            var foldoutTracker = meshDetail;
            var foldout = Helpers.IsTrackedFoldout(foldoutTracker);
            foldout = EditorGUILayout.Foldout(foldout, $"{index} - {meshDetail.Transform.name}");
            if (foldout)
            {
                ShowMeshDetails(meshDetail);
                Helpers.AddFoldoutTracking(foldoutTracker);
            }
            else
            {
                Helpers.RemoveFoldoutTracking(foldoutTracker);
            }

            EditorGUI.indentLevel--;
        }

        public static void ShowMeshDetails(MeshDetails meshDetail)
        {
            EditorGUI.indentLevel++;

            ShowMeshDetails(meshDetail.OriginalSharedMesh, "Original Shared Mesh", meshDetail.OriginalSharedMesh == meshDetail.MeshFilter.sharedMesh);
            ShowMeshDetails(meshDetail.SlicedMesh, "Sliced Mesh", meshDetail.SlicedMesh == meshDetail.MeshFilter.sharedMesh);

            EditorGUILayout.LabelField($"World Space Bounds: {meshDetail.MeshRenderer.bounds.ToString("0.00")}");
            EditorGUILayout.LabelField($"Original Bounds: {(meshDetail.OriginalBounds.HasValue ? meshDetail.OriginalBounds.Value.ToString("0.00") : "NULL")}");
            EditorGUILayout.LabelField($"Vert Hash: {meshDetail.SlicedVertHash}");
            EditorGUILayout.LabelField($"UV Hash: {meshDetail.SlicedUvHash}");

            EditorGUI.indentLevel--;
        }

        public static void ShowMeshDetails(Mesh mesh, string name, bool inUse)
        {
            if (mesh == null)
            {
                EditorGUILayout.LabelField($"{name}: NULL"); 
            }
            else
            {
                var inUseString = inUse ? "*" : string.Empty;
                EditorGUILayout.LabelField($"{name}: {mesh.name}{inUseString}");

                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField($"Sub Mesh Count: {mesh.subMeshCount}");

                EditorGUI.indentLevel++;

                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    EditorGUILayout.LabelField($"Sub Mesh {i} Index Count: {mesh.GetIndexCount(i)}");
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.LabelField($"Vertex Count: {mesh.vertexCount}");
                EditorGUILayout.LabelField($"Bounds: {mesh.bounds.ToString("0.00")}");

                EditorGUI.indentLevel--;
            }
        }

        protected override void HelpBox()
        {
            base.HelpBox();

            MeshSlicerComponent meshSlicerComponent = (MeshSlicerComponent)target;

            var meshDetailsList = meshSlicerComponent.MeshDetailsList;
            var managedMeshCount = meshDetailsList.Count;
            if (managedMeshCount == 0)
            {
                EditorGUILayout.HelpBox($"There are no meshes being sliced by this component.\nEnsure that any child components you want to slice have both a {nameof(MeshRenderer)} and {nameof(MeshFilter)}, and it or it's ancestors do not have a {nameof(SlicerIgnore)} or {nameof(SlicerController)} component.", MessageType.Info);
            }

            var readOnlyMeshNames = "";
            var readOnlyCount = 0;
            var noValidUVsNames = "";
            var noValidUVsCount = 0;
            var noValidTopologyNames = "";
            var noValidTopologyCount = 0;
            foreach (var meshDetail in meshDetailsList)
            {
                if (meshDetail.OriginalSharedMesh == null)
                {
                    continue;
                }

                if (!meshDetail.OriginalSharedMesh.isReadable)
                {
                    readOnlyCount++;
                    readOnlyMeshNames = $"{readOnlyMeshNames}\n{readOnlyCount} - {meshDetail.OriginalSharedMesh.name}";
                }

                if (!meshDetail.SlicedUvHash.isValid)
                {
                    noValidUVsCount++;
                    noValidUVsNames = $"{noValidUVsNames}\n{noValidUVsCount} - {meshDetail.OriginalSharedMesh.name}";
                }

                for (int i2 = 0; i2 < meshDetail.OriginalSharedMesh.subMeshCount; i2++)
                {
                    var subMeshTopology = meshDetail.OriginalSharedMesh.GetTopology(i2);

                    if (subMeshTopology != MeshTopology.Triangles)
                    {
                        noValidTopologyCount++;
                        noValidTopologyNames = $"{noValidTopologyNames}\n{noValidTopologyCount} - {meshDetail.OriginalSharedMesh.name}";
                        break;
                    }
                }
            }

            if (readOnlyCount > 0)
            {
                EditorGUILayout.HelpBox($"{readOnlyCount} read only meshes cannot be sliced. The 'Read/Write Enabled' setting must be checked in the Model Import Settings window.{readOnlyMeshNames}", MessageType.Warning);
            }

            if (skipUvsProperty.boolValue == false && noValidUVsCount == managedMeshCount && managedMeshCount > 0)
            {
                EditorGUILayout.HelpBox("The sliced meshes do not have UVs that can be scaled.\n" +
                    $"If UV scaling is desired ensure that the textures Wrap Mode is set to 'Repeat' or 'Mirror' and that meshes have one Material per Sub Mesh.\n" +
                    $"If UV scaling is not desired the '{skipUvsProperty.displayName}' checkbox can be checked." +
                    $"{noValidUVsNames}",
                    MessageType.Info);
            }

            if (skipUvsProperty.boolValue == false && noValidTopologyCount > 0)
            {
                EditorGUILayout.HelpBox($"UV slicing requires the topology of the mesh to be Triangles.{noValidTopologyNames}", MessageType.Warning);
            }
        }

        [MenuItem("CONTEXT/MeshSlicerComponent/Export Sliced Mesh as Asset", false, 1251)]
        public static void SwapScaleWithSize(MenuCommand command)
        {
            ExportMeshWindow.Init();
        }
    }
}
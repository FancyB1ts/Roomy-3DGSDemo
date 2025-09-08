// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

namespace Slicer.Editor
{
    [CustomEditor(typeof(ColliderSlicerComponent))]
    [CanEditMultipleObjects]
    public class ColliderSlicerComponentEditor : SlicerComponentEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
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
            var colliderSlicerComponent = (ColliderSlicerComponent)slicerComponent;
            EditorGUI.indentLevel++;

            base.ShowDetails(colliderSlicerComponent);

            ShowDetails("Mesh Collider Details", colliderSlicerComponent, colliderSlicerComponent.MeshColliderDetailsList);
            ShowDetails("Mesh Slicer Managed Mesh Collider Details", colliderSlicerComponent, colliderSlicerComponent.MeshSlicerManagedMeshColliderDetailsList);
            ShowDetails("Box Collider Details", colliderSlicerComponent, colliderSlicerComponent.BoxColliderDetailsList);
            ShowDetails("Unsupported Collider Details", colliderSlicerComponent, colliderSlicerComponent.UnsupportedColliderDetailsList);

            EditorGUI.indentLevel--;
        }

        private static void ShowDetails<T>(string colliderTypeName, ColliderSlicerComponent colliderSlicerComponent, ReadOnlyCollection<T> colliderDetailsList) where T : ColliderDetails
        {
            if (colliderDetailsList == null)
            {
                return;
            }

            var managedCollidersCount = colliderDetailsList.Count;
            if (managedCollidersCount == 0)
            {
                return;
            }

            EditorGUILayout.LabelField($"Managed Count: {managedCollidersCount}");

            var foldoutTracker = (colliderSlicerComponent, colliderDetailsList.GetType());
            var foldout = Helpers.IsTrackedFoldout(foldoutTracker);
            foldout = EditorGUILayout.Foldout(foldout, colliderTypeName);
            if (foldout)
            {
                for (int i = 0; i < managedCollidersCount; i++)
                {
                    T colliderDetailProperty = colliderDetailsList[i];

                    ShowColliderDetails<T>(colliderDetailProperty, i);
                }
                Helpers.AddFoldoutTracking(foldoutTracker);
            }
            else
            {
                Helpers.RemoveFoldoutTracking(foldoutTracker);
            }
        }

        private static void ShowColliderDetails<T>(T colliderDetail, int index) where T : ColliderDetails
        {
            EditorGUI.indentLevel++;

            var foldoutTracker = colliderDetail;
            var foldout = Helpers.IsTrackedFoldout(foldoutTracker);
            foldout = EditorGUILayout.Foldout(foldout, $"{index} - {colliderDetail.Transform.name}");
            if (foldout)
            {
                ShowColliderDetails(colliderDetail);
                Helpers.AddFoldoutTracking(foldoutTracker);
            }
            else
            {
                Helpers.RemoveFoldoutTracking(foldoutTracker);
            }

            EditorGUI.indentLevel--;
        }

        private static void ShowColliderDetails<T>(T colliderDetail) where T : ColliderDetails
        {
            if (colliderDetail is MeshColliderDetails mcd)
            {
                ShowColliderDetails(mcd);
            }
            else if (colliderDetail is MeshSlicerManagedMeshColliderDetails msmmcd)
            {
                ShowColliderDetails(msmmcd);
            }
            else if (colliderDetail is BoxColliderDetails bcd)
            {
                ShowColliderDetails(bcd);
            }
            else if (colliderDetail is UnsupportedColliderDetails ucd)
            {
                ShowColliderDetails(ucd);
            }
        }

        private static void ShowColliderDetails(MeshColliderDetails mcd)
        {
            EditorGUI.indentLevel++;

            MeshSlicerComponentEditor.ShowMeshDetails(mcd.OriginalSharedMesh, "Original Shared Mesh", mcd.OriginalSharedMesh == mcd.MeshCollider.sharedMesh);
            MeshSlicerComponentEditor.ShowMeshDetails(mcd.SlicedMesh, "Sliced Mesh", mcd.SlicedMesh == mcd.MeshCollider.sharedMesh);

            EditorGUILayout.LabelField($"Original Bounds: {mcd.OriginalBounds.ToString("0.00")}");
            EditorGUILayout.LabelField($"Vert Hash: {mcd.SlicedVertHash}");

            EditorGUI.indentLevel--;
        }

        private static void ShowColliderDetails(MeshSlicerManagedMeshColliderDetails mcd)
        {
            EditorGUI.indentLevel++;

            MeshSlicerComponentEditor.ShowMeshDetails(mcd.MeshSlicerDetails);

            EditorGUI.indentLevel--;
        }

        private static void ShowColliderDetails(BoxColliderDetails bcd)
        {
            EditorGUI.indentLevel++;

            var bounds = new Bounds(bcd.BoxCollider.center, bcd.BoxCollider.size);
            EditorGUILayout.LabelField($"Bounds: {bounds.ToString("0.00")}");
            EditorGUILayout.LabelField($"World Space Bounds: {bcd.BoxCollider.bounds.ToString("0.00")}");
            EditorGUILayout.LabelField($"Original Bounds: {bcd.OriginalBounds.ToString("0.00")}");
            EditorGUILayout.LabelField($"Original Collider Properties: {bcd.OriginalColliderProperties.ToString("0.00")}");

            EditorGUI.indentLevel--;
        }

        private static void ShowColliderDetails(UnsupportedColliderDetails ucd)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField($"Type: {ucd.Collider.GetType().Name}");

            EditorGUI.indentLevel--;
        }

        protected override void HelpBox()
        {
            base.HelpBox();

            ColliderSlicerComponent colliderSlicerComponent = (ColliderSlicerComponent)target;

            var meshColliderDetailsList = colliderSlicerComponent.MeshColliderDetailsList;
            var meshColliderDetailsCount = meshColliderDetailsList.Count;

            var meshSlicerManagedMeshColliderDetails = colliderSlicerComponent.MeshSlicerManagedMeshColliderDetailsList;
            var meshSlicerManagedMeshColliderDetailsCount = meshSlicerManagedMeshColliderDetails.Count;

            var boxColliderDetails = colliderSlicerComponent.BoxColliderDetailsList;
            var boxColliderDetailsCount = boxColliderDetails.Count;

            var unsupportedColliderDetails = colliderSlicerComponent.UnsupportedColliderDetailsList;
            var unsupportedColliderDetailsCount = unsupportedColliderDetails.Count;

            var allColliderDetailsCount = meshColliderDetailsCount + meshSlicerManagedMeshColliderDetailsCount + boxColliderDetailsCount + unsupportedColliderDetailsCount;

            if (allColliderDetailsCount == 0)
            {
                EditorGUILayout.HelpBox($"There are no colliders being sliced by this component.\nEnsure that any child components you want to slice have both a {nameof(Collider)} and it or it's ancestors do not have a {nameof(SlicerIgnore)} or {nameof(SlicerController)} component.", MessageType.Info);
            }

            var readOnlyMeshNames = "";
            var readOnlyCount = 0;

            foreach (var mcd in meshColliderDetailsList)
            {
                if (mcd.OriginalSharedMesh == null)
                {
                    continue;
                }

                if (!mcd.OriginalSharedMesh.isReadable)
                {
                    readOnlyCount++;
                    readOnlyMeshNames = $"{readOnlyMeshNames}\n{readOnlyCount} - {mcd.OriginalSharedMesh.name}";
                }
            }

            if (readOnlyCount > 0)
            {
                EditorGUILayout.HelpBox($"{readOnlyCount} read only meshes cannot be sliced. 'Read/Write Enabled' setting must be checked in the Model Import Settings window.{readOnlyMeshNames}", MessageType.Warning);
            }

            var unsupportedUnconvertedNames = "";
            var unsupportedUnconvertedCount = 0;

            foreach (var ucd in unsupportedColliderDetails)
            {
                unsupportedUnconvertedCount++;
                unsupportedUnconvertedNames = $"{unsupportedUnconvertedNames}\n{unsupportedUnconvertedCount} - {ucd.Collider.name} ({ucd.Collider.GetType().Name})";
            }

            if (unsupportedUnconvertedCount > 0)
            {
                var msg = $"{unsupportedUnconvertedCount} colliders cannot be sliced. These colliders cannot be converted to a simplified mesh collider. If you want them to be sliced, convert them to mesh colliders";

                msg += unsupportedUnconvertedNames;

                EditorGUILayout.HelpBox(msg, MessageType.Info);
            }
        }
    }
}
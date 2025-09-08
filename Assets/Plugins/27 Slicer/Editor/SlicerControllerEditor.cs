// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Slicer.Editor
{
    [CustomEditor(typeof(SlicerController))]
    [CanEditMultipleObjects]
    public class SlicerControllerEditor : UnityEditor.Editor
    {
        protected SerializedProperty scriptProperty;

        private SerializedProperty slicesProperty;

        private static HashSet<SlicerController> EditModeCollection;

        private void OnEnable()
        {
            scriptProperty = serializedObject.FindProperty("m_Script");

            if (EditModeCollection == null)
            {
                EditModeCollection = new HashSet<SlicerController>();
            }

            slicesProperty = serializedObject.FindProperty(nameof(SlicerController.Slices));

            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;

            AssemblyReloadEvents.beforeAssemblyReload -= AssemblyReloadEvents_beforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += AssemblyReloadEvents_beforeAssemblyReload;
        }

        private static void EditorApplication_playModeStateChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.ExitingEditMode)
            {
                AllExitEditMode();
            }
        }

        private static void AssemblyReloadEvents_beforeAssemblyReload()
        {
            AllExitEditMode();
        }

        public override void OnInspectorGUI()
        {
            if (SwapScaleWithSizeHotkeyIsPressed())
            {
                foreach (var t in targets)
                {
                    var s = (SlicerController)t;
                    SwapScaleWithSize(s);
                }
            }

            DrawGui();

            DetailsBox();

            HelpBox();
        }

        private void DrawGui()
        {
            DrawDefaultInspector();

            if (!slicesProperty.hasMultipleDifferentValues)
            {
                var boundsValue = slicesProperty.vector3Value;
                boundsValue = Vector3.Min(boundsValue, Vector3.one);
                boundsValue = Vector3.Max(boundsValue, Vector3.zero);
                if (slicesProperty.vector3Value != boundsValue)
                {
                    slicesProperty.vector3Value = boundsValue;
                }
            }

            serializedObject.ApplyModifiedProperties();

            var slicer = (SlicerController)target;
            if (slicer.isActiveAndEnabled)
            {
                EditorGUILayout.Space();

                if (!Application.isPlaying)
                {
                    var editMode = GetEditMode(slicer);
                    var guiContent = new GUIContent($"{(editMode ? "Disable" : "Enable")} Edit Mode", "Toggles Edit Mode on and off.\nToggle All Slices: Ctrl + Alt + S\nToggle This Slice: Alt + S");
                    if (GUILayout.Button(guiContent) || EditModeHotkeyIsPressed())
                    {
                        SetEditMode(!editMode);
                    }
                }
                else
                {
                    var guiContent = new GUIContent("Refresh Slice", "Re-slices the items that are controlled by this Slicer Controller. (Alt + S)");
                    if (GUILayout.Button(guiContent) || RefreshHotkeyIsPressed())
                    {
                        RefreshSlice();
                    }
                }

            }
        }

        [MenuItem("Window/27 Slicer/Toggle Edit Mode for All Slices %&s", false, 1251)]
        static void ToggleAllSlices(MenuCommand menuCommand)
        {
            // If there are any Slicer Controllers in Edit Mode, toggle them off
            // Otherwise toggle them all on.
            if (EditModeCollection.Count > 0)
            {
                AllExitEditMode();
            }
            else
            {
                AllEnterEditMode();
            }
        }

        [MenuItem("Window/27 Slicer/Refresh All Slices %&r", false, 1251)]
        static void RefreshAllSlices(MenuCommand menuCommand)
        {
            var allSlicerControllers = GameObject.FindObjectsOfType<SlicerController>();

            foreach (var slicerController in allSlicerControllers)
            {
                RefreshSlice(slicerController);
            }
        }

        public bool EditModeHotkeyIsPressed()
        {
            Event current = Event.current;
            if (current.type == EventType.KeyDown && current.modifiers == EventModifiers.Alt && current.keyCode == KeyCode.S)
            {
                current.Use();
                return true;
            }

            return false;
        }

        public bool RefreshHotkeyIsPressed()
        {
            Event current = Event.current;
            if (current.type == EventType.KeyDown && current.modifiers == EventModifiers.Alt && current.keyCode == KeyCode.R)
            {
                current.Use();
                return true;
            }

            return false;
        }

        [MenuItem("CONTEXT/SlicerController/Swap Scale With Size", false, 1251)]
        public static void SwapScaleWithSize(MenuCommand command)
        {
            var slicerController = command.context as SlicerController;
            SwapScaleWithSize(slicerController);
        }

        [MenuItem("CONTEXT/SlicerController/Finalize Slicing", false, 1252)]
        public static void FinalizeSlicerController(MenuCommand command)
        {
            var slicerController = command.context as SlicerController;

            var okClicked = EditorUtility.DisplayDialog("Finalize Slicing", "Are you sure you wish to Finalize Slicing?\n\nThis action cannot be undone.", "Finalize Slicing", "Cancel");

            if (okClicked)
            {
                slicerController.FinalizeSlicing();
            }
        }

        public static void SwapScaleWithSize(SlicerController slicerController)
        {
            var transform = slicerController.transform;
            var size = slicerController.Size;
            slicerController.Size = transform.localScale;
            transform.localScale = size;
        }

        public bool SwapScaleWithSizeHotkeyIsPressed()
        {
            Event current = Event.current;
            if (current.type == EventType.KeyDown && current.modifiers == EventModifiers.Alt && current.keyCode == KeyCode.Slash)
            {
                current.Use();
                return true;
            }

            return false;
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
                            ShowDetails((SlicerController)target);
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
                    ShowDetails((SlicerController)target);
                }
            }
        }

        public void ShowDetails(SlicerController target)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField($"Offset: {target.Offset}");
            EditorGUILayout.LabelField($"Sliced Bounds: {target.SlicedBounds}");
            EditorGUILayout.LabelField($"Complete Bounds: {target.CompleteBounds}");
            EditorGUILayout.LabelField($"Previous Hash: {target.PreviousHash}");

            var slicerComponents = target.SlicerComponents;
            if (slicerComponents != null && slicerComponents.Any())
            {
                var foldoutTracker = (target, slicerComponents.GetType());
                var foldout = Helpers.IsTrackedFoldout(foldoutTracker);
                foldout = EditorGUILayout.Foldout(foldout, "Slicer Components");
                if (foldout)
                {
                    EditorGUI.indentLevel++;

                    foreach (var slicerComponent in slicerComponents)
                    {
                        EditorGUILayout.LabelField($"{slicerComponent.GetType().Name}: {slicerComponent.gameObject.name}");
                    }

                    EditorGUI.indentLevel--;
                    Helpers.AddFoldoutTracking(foldoutTracker);
                }
                else
                {
                    Helpers.RemoveFoldoutTracking(foldoutTracker);
                }
            }

            var sliceModifiers = target.SliceModifiers;
            if (sliceModifiers != null && sliceModifiers.Any())
            {
                var foldoutTracker = (target, sliceModifiers.GetType());
                var foldout = Helpers.IsTrackedFoldout(foldoutTracker);
                foldout = EditorGUILayout.Foldout(foldout, "Slice Modifiers");
                if (foldout)
                {
                    EditorGUI.indentLevel++;

                    foreach (var sliceModifier in sliceModifiers)
                    {
                        EditorGUILayout.LabelField($"{sliceModifier.GetType().Name}: {sliceModifier.gameObject.name}");
                    }

                    EditorGUI.indentLevel--;
                    Helpers.AddFoldoutTracking(foldoutTracker);
                }
                else
                {
                    Helpers.RemoveFoldoutTracking(foldoutTracker);
                }
            }

            var slicerIgnores = target.SlicerIgnores;
            if (slicerIgnores != null && slicerIgnores.Any())
            {
                var foldoutTracker = (target, slicerIgnores.GetType());
                var foldout = Helpers.IsTrackedFoldout(foldoutTracker);
                foldout = EditorGUILayout.Foldout(foldout, "Slicer Ignores");
                if (foldout)
                {
                    EditorGUI.indentLevel++;

                    foreach (var slicerIgnore in slicerIgnores)
                    {
                        EditorGUILayout.LabelField($"{slicerIgnore.GetType().Name}: {slicerIgnore.gameObject.name}");
                    }

                    EditorGUI.indentLevel--;
                    Helpers.AddFoldoutTracking(foldoutTracker);
                }
                else
                {
                    Helpers.RemoveFoldoutTracking(foldoutTracker);
                }
            }

            EditorGUI.indentLevel--;
        }

        private static bool GetEditMode(SlicerController slicer)
        {
            var editMode = EditModeCollection.Contains(slicer);
            return editMode;
        }

        private void SetEditMode(bool editMode)
        {
            foreach (var target in targets)
            {
                var slicer = (SlicerController)target;
                var currentEditMode = GetEditMode(slicer);

                if (editMode != currentEditMode)
                {
                    SetEditMode(slicer, editMode);
                }
            }
        }

        private static void AllExitEditMode()
        {
            foreach (var slicer in EditModeCollection)
            {
                SetEditMode(slicer, false);
            }

            EditModeCollection.Clear();
        }

        private static void AllEnterEditMode()
        {
            EditModeCollection.Clear();
            var allSlicerControllers = GameObject.FindObjectsOfType<SlicerController>();

            foreach (var slicer in allSlicerControllers)
            {
                SetEditMode(slicer, true);
            }
        }

        private static void SetEditMode(SlicerController slicer, bool editMode)
        {
            if (editMode)
            {
                EditModeCollection.Add(slicer);
                slicer.DisableSlicing();
            }
            else
            {
                EditModeCollection.Remove(slicer);
                slicer.EnableSlicing();
            }

            RefreshSlice(slicer);
        }

        private void RefreshSlice()
        {
            foreach (var target in targets)
            {
                var slicer = (SlicerController)target;

                RefreshSlice(slicer);
            }
        }

        private static void RefreshSlice(SlicerController slicer)
        {
            slicer.RefreshSliceImmediate();
        }

        private void HelpBox()
        {
            var slicer = (SlicerController)target;

            var slicerComponents = slicer.SlicerComponents;
            if (slicerComponents != null) // Don't show if we havn't tried to slice yet
            {
                if (slicerComponents.Count == 0)
                {
                    EditorGUILayout.HelpBox("No Slicer Components have been assigned.\nAdd a Slicer Component to get started.", MessageType.Warning);
                }
                else if (!slicer.CompleteBounds.HasValue)
                {
                    EditorGUILayout.HelpBox("None of the assigned Slicer Components has provided any bounds.\nCheck the assigned Slicer Components for more information.", MessageType.Warning);
                }
            }
        }

        private void OnSceneGUI()
        {
            var slicer = (SlicerController)target;
            var editMode = GetEditMode(slicer);
            if (!editMode || slicer == null || !slicer.CompleteBounds.HasValue)
            {
                return;
            }

            DrawSlices(slicer);

            DrawSize(slicer);
        }

        private void DrawSlices(SlicerController slicer)
        {
            var bounds = slicer.CompleteBounds.Value;
            var boundsExtents = Vector3.one * 0.5f;
            var sliceExtents = slicer.Slices * 0.5f;

            var boundsMatrix = Matrix4x4.TRS(bounds.center, Quaternion.identity, bounds.size);
            var rootMatrix = slicer.transform.localToWorldMatrix;
            var handlesMatrix = rootMatrix * boundsMatrix;

            float handleSize = HandleUtility.GetHandleSize(slicer.transform.position);
            Handles.CapFunction capFunction;
            var color = Color.red;
            Handles.color = color;

            var slicesValue = slicer.Slices;
            var positions = new Vector3[4];

            EditorGUI.BeginChangeCheck();

            #region X

            capFunction = (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType) =>
            {
                positions[0] = handlesMatrix.MultiplyPoint3x4(new Vector3(-sliceExtents.x, boundsExtents.y, -boundsExtents.z));
                positions[1] = handlesMatrix.MultiplyPoint3x4(new Vector3(-sliceExtents.x, boundsExtents.y, boundsExtents.z));
                positions[2] = handlesMatrix.MultiplyPoint3x4(new Vector3(-sliceExtents.x, -boundsExtents.y, boundsExtents.z));
                positions[3] = handlesMatrix.MultiplyPoint3x4(new Vector3(-sliceExtents.x, -boundsExtents.y, -boundsExtents.z));

                DrawHandleCap(controlID, position, rotation, size, positions, color, eventType, Vector3.right);
            };
            slicesValue.x = Handles.ScaleValueHandle(slicesValue.x, handlesMatrix.MultiplyPoint3x4(new Vector3(-sliceExtents.x, 0f, 0f)), handlesMatrix.rotation * Quaternion.Euler(0f, -90f, 0f), handleSize, capFunction, 0.01f);

            capFunction = (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType) =>
            {
                positions[0] = handlesMatrix.MultiplyPoint3x4(new Vector3(sliceExtents.x, boundsExtents.y, -boundsExtents.z));
                positions[1] = handlesMatrix.MultiplyPoint3x4(new Vector3(sliceExtents.x, boundsExtents.y, boundsExtents.z));
                positions[2] = handlesMatrix.MultiplyPoint3x4(new Vector3(sliceExtents.x, -boundsExtents.y, boundsExtents.z));
                positions[3] = handlesMatrix.MultiplyPoint3x4(new Vector3(sliceExtents.x, -boundsExtents.y, -boundsExtents.z));

                DrawHandleCap(controlID, position, rotation, size, positions, color, eventType, -Vector3.right);
            };
            slicesValue.x = Handles.ScaleValueHandle(slicesValue.x, handlesMatrix.MultiplyPoint3x4(new Vector3(sliceExtents.x, 0f, 0f)), handlesMatrix.rotation * Quaternion.Euler(0f, 90f, 0f), handleSize, capFunction, 0.01f);

            #endregion

            #region Y

            capFunction = (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType) =>
            {
                positions[0] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, sliceExtents.y, -boundsExtents.z));
                positions[1] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, sliceExtents.y, boundsExtents.z));
                positions[2] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, sliceExtents.y, boundsExtents.z));
                positions[3] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, sliceExtents.y, -boundsExtents.z));

                DrawHandleCap(controlID, position, rotation, size, positions, color, eventType, Vector3.up);
            };
            slicesValue.y = Handles.ScaleValueHandle(slicesValue.y, handlesMatrix.MultiplyPoint3x4(new Vector3(0f, sliceExtents.y, 0f)), handlesMatrix.rotation * Quaternion.Euler(-90f, 0f, 0f), handleSize, capFunction, 0.01f);

            capFunction = (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType) =>
            {
                positions[0] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, -sliceExtents.y, -boundsExtents.z));
                positions[1] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, -sliceExtents.y, boundsExtents.z));
                positions[2] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, -sliceExtents.y, boundsExtents.z));
                positions[3] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, -sliceExtents.y, -boundsExtents.z));

                DrawHandleCap(controlID, position, rotation, size, positions, color, eventType, -Vector3.up);
            };
            slicesValue.y = Handles.ScaleValueHandle(slicesValue.y, handlesMatrix.MultiplyPoint3x4(new Vector3(0f, -sliceExtents.y, 0f)), handlesMatrix.rotation * Quaternion.Euler(90f, 0f, 0f), handleSize, capFunction, 0.01f);

            #endregion

            #region Z

            capFunction = (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType) =>
            {
                positions[0] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, boundsExtents.y, -sliceExtents.z));
                positions[1] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, boundsExtents.y, -sliceExtents.z));
                positions[2] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, -boundsExtents.y, -sliceExtents.z));
                positions[3] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, -boundsExtents.y, -sliceExtents.z));

                DrawHandleCap(controlID, position, rotation, size, positions, color, eventType, Vector3.forward);
            };
            slicesValue.z = Handles.ScaleValueHandle(slicesValue.z, handlesMatrix.MultiplyPoint3x4(new Vector3(0f, 0f, -sliceExtents.z)), handlesMatrix.rotation * Quaternion.Euler(0f, 180f, 0f), handleSize, capFunction, 0.01f);

            capFunction = (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType) =>
            {
                positions[0] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, boundsExtents.y, sliceExtents.z));
                positions[1] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, boundsExtents.y, sliceExtents.z));
                positions[2] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, -boundsExtents.y, sliceExtents.z));
                positions[3] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, -boundsExtents.y, sliceExtents.z));

                DrawHandleCap(controlID, position, rotation, size, positions, color, eventType, -Vector3.forward);
            };
            slicesValue.z = Handles.ScaleValueHandle(slicesValue.z, handlesMatrix.MultiplyPoint3x4(new Vector3(0f, 0f, sliceExtents.z)), handlesMatrix.rotation * Quaternion.Euler(0f, 0f, 0f), handleSize, capFunction, 0.01f);

            #endregion

            slicesValue = Vector3.Min(slicesValue, Vector3.one);
            slicesValue = Vector3.Max(slicesValue, Vector3.zero);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Slices Value");
                slicer.Slices = slicesValue;
            }
        }

        private void DrawSize(SlicerController slicer)
        {
            var bounds = slicer.CompleteBounds.Value;
            var boundsExtents = Vector3.one * 0.5f;

            var boundsMatrix = Matrix4x4.TRS(bounds.center, Quaternion.identity, Vector3.Scale(bounds.size, slicer.Size));
            var rootMatrix = slicer.transform.localToWorldMatrix;
            var handlesMatrix = rootMatrix * boundsMatrix;

            float handleSize = HandleUtility.GetHandleSize(slicer.transform.position);
            Handles.CapFunction capFunction;
            var color = Color.yellow;
            Handles.color = color;

            var sizeValue = slicer.Size;
            var positions = new Vector3[4];

            EditorGUI.BeginChangeCheck();

            #region X

            capFunction = (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType) =>
            {
                positions[0] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, boundsExtents.y, -boundsExtents.z));
                positions[1] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, boundsExtents.y, boundsExtents.z));
                positions[2] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, -boundsExtents.y, boundsExtents.z));
                positions[3] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, -boundsExtents.y, -boundsExtents.z));

                DrawHandleCap(controlID, position, rotation, size, positions, color, eventType, Vector3.right);
            };
            sizeValue.x = Handles.ScaleValueHandle(sizeValue.x, handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, 0f, 0f)), handlesMatrix.rotation * Quaternion.Euler(0f, -90f, 0f), handleSize, capFunction, 0.01f);

            capFunction = (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType) =>
            {
                positions[0] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, boundsExtents.y, -boundsExtents.z));
                positions[1] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, boundsExtents.y, boundsExtents.z));
                positions[2] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, -boundsExtents.y, boundsExtents.z));
                positions[3] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, -boundsExtents.y, -boundsExtents.z));

                DrawHandleCap(controlID, position, rotation, size, positions, color, eventType, -Vector3.right);
            };
            sizeValue.x = Handles.ScaleValueHandle(sizeValue.x, handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, 0f, 0f)), handlesMatrix.rotation * Quaternion.Euler(0f, 90f, 0f), handleSize, capFunction, 0.01f);

            #endregion

            #region Y

            capFunction = (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType) =>
            {
                positions[0] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, boundsExtents.y, -boundsExtents.z));
                positions[1] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, boundsExtents.y, boundsExtents.z));
                positions[2] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, boundsExtents.y, boundsExtents.z));
                positions[3] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, boundsExtents.y, -boundsExtents.z));

                DrawHandleCap(controlID, position, rotation, size, positions, color, eventType, Vector3.up);
            };
            sizeValue.y = Handles.ScaleValueHandle(sizeValue.y, handlesMatrix.MultiplyPoint3x4(new Vector3(0f, boundsExtents.y, 0f)), handlesMatrix.rotation * Quaternion.Euler(-90f, 0f, 0f), handleSize, capFunction, 0.01f);

            capFunction = (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType) =>
            {
                positions[0] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, -boundsExtents.y, -boundsExtents.z));
                positions[1] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, -boundsExtents.y, boundsExtents.z));
                positions[2] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, -boundsExtents.y, boundsExtents.z));
                positions[3] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, -boundsExtents.y, -boundsExtents.z));

                DrawHandleCap(controlID, position, rotation, size, positions, color, eventType, -Vector3.up);
            };
            sizeValue.y = Handles.ScaleValueHandle(sizeValue.y, handlesMatrix.MultiplyPoint3x4(new Vector3(0f, -boundsExtents.y, 0f)), handlesMatrix.rotation * Quaternion.Euler(90f, 0f, 0f), handleSize, capFunction, 0.01f);

            #endregion

            #region Z

            capFunction = (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType) =>
            {
                positions[0] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, boundsExtents.y, -boundsExtents.z));
                positions[1] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, boundsExtents.y, -boundsExtents.z));
                positions[2] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, -boundsExtents.y, -boundsExtents.z));
                positions[3] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, -boundsExtents.y, -boundsExtents.z));

                DrawHandleCap(controlID, position, rotation, size, positions, color, eventType, Vector3.forward);
            };
            sizeValue.z = Handles.ScaleValueHandle(sizeValue.z, handlesMatrix.MultiplyPoint3x4(new Vector3(0f, 0f, -boundsExtents.z)), handlesMatrix.rotation * Quaternion.Euler(0f, 180f, 0f), handleSize, capFunction, 0.01f);

            capFunction = (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType) =>
            {
                positions[0] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, boundsExtents.y, boundsExtents.z));
                positions[1] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, boundsExtents.y, boundsExtents.z));
                positions[2] = handlesMatrix.MultiplyPoint3x4(new Vector3(boundsExtents.x, -boundsExtents.y, boundsExtents.z));
                positions[3] = handlesMatrix.MultiplyPoint3x4(new Vector3(-boundsExtents.x, -boundsExtents.y, boundsExtents.z));

                DrawHandleCap(controlID, position, rotation, size, positions, color, eventType, -Vector3.forward);
            };
            sizeValue.z = Handles.ScaleValueHandle(sizeValue.z, handlesMatrix.MultiplyPoint3x4(new Vector3(0f, 0f, boundsExtents.z)), handlesMatrix.rotation * Quaternion.Euler(0f, 0f, 0f), handleSize, capFunction, 0.01f);

            #endregion

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Size Value");
                slicer.Size = sizeValue;
            }
        }

        private static Color GetHandleCapColor(Color color, Vector3 position, Vector3 axisVector)
        {
            Camera current = Camera.current;
            Vector3 cameraViewFrom = current.orthographic ? (-current.transform.forward).normalized : (position - current.transform.position).normalized;
            var lerp = Mathf.Clamp01(6.666668f * (Mathf.Abs(Vector3.Dot(cameraViewFrom, axisVector)) - 0.85f));
            var lerpColor = Color.Lerp(color, Color.clear, lerp);
            return lerpColor;
        }

        private static void DrawHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, Vector3[] rectVerts, Color color, EventType eventType, Vector3 axisVector)
        {
            var origColor = Handles.color;

            Color faceColor;
            Color handleCapColor;
            if (origColor == color)
            {
                faceColor = Color.clear;
                handleCapColor = GetHandleCapColor(origColor, position, axisVector);
            }
            else
            {
                faceColor = new Color(origColor.r, origColor.g, origColor.b, 0.05f);
                handleCapColor = origColor;
            }
            var outlineColor = Handles.color;
            Handles.DrawSolidRectangleWithOutline(rectVerts, faceColor, outlineColor);

            Handles.color = handleCapColor;
            var offsetPosition = position + (axisVector * (size * 0.5f));
            Handles.ConeHandleCap(controlID, offsetPosition, rotation, size, eventType);
            Handles.color = origColor;
        }
    }
}
// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Slicer.Editor
{
    public class ExportMeshWindow : EditorWindow
    {
        private SlicerExportConfiguration slicerExportConfiguration;

        private Dictionary<int, ExportDetails> exportDetailsList = new Dictionary<int, ExportDetails>();
        private Vector2 scrollPosition;

        private const string windowName = "Sliced Mesh Exporter";

        #region Configuration File Info
        /// <summary>
        /// The name of the configuration file.
        /// </summary>
        private const string configurationFileName = "ExportConfiguration";
        /// <summary>
        /// The name of the configuration file including the file extension.
        /// </summary>
        private const string configurationFileFullName = configurationFileName + ".asset";
        /// <summary>
        /// The directory of the configuration file
        /// </summary>
        private const string configurationDirectory = "ProjectSettings/27Slicer/";
        /// <summary>
        /// The directory of the configuration file
        /// </summary>
        private const string configurationFilePath = configurationDirectory + configurationFileFullName;
        #endregion

        private GUIStyle resetPathStyle;
        private GUIStyle titleStyle;
        private GUIStyle dropDownBoxStyle;
        private GUIStyle helpButtonStyle;

        [MenuItem("Window/27 Slicer/" + windowName, false, 1211)]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow<ExportMeshWindow>(true, windowName, true);
            window.minSize = new Vector2(1000, 500);
            window.Show();
        }

        private void OnEnable()
        {
            LoadConfiguration();
            Selection.selectionChanged += SelectionChanged;
            SelectionChanged();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= SelectionChanged;
        }

        private void SelectionChanged()
        {
            UpdateFileList();
        }

        private void UpdateFileList()
        {
            exportDetailsList = Selection.gameObjects
                .Select(e => GetExportDetails(e.transform))
                .Where(e => e != null)
                .SelectMany(e => e)
                .ToDictionary(e => e.Id);

            var groupedPathsCollection = exportDetailsList.Values.GroupBy(e => e.AssetPath);
            foreach (var groupedPaths in groupedPathsCollection)
            {
                if (groupedPaths.Count() > 1)
                {
                    var count = 1;
                    foreach (var groupedPath in groupedPaths)
                    {
                        var index = groupedPath.AssetPath.LastIndexOf('.');

                        if (index == -1)
                        {
                            groupedPath.AssetPath += $"_{count}";
                        }
                        else
                        {
                            groupedPath.AssetPath = groupedPath.AssetPath.Insert(index, $"_{count}");
                        }
                        count++;
                    }
                }
            }

            Repaint();
        }

        private void SetStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 20,
                };
            }

            if (resetPathStyle == null)
            {
                resetPathStyle = new GUIStyle(GUI.skin.button)
                {
                    fixedWidth = 50
                };
            }

            if (dropDownBoxStyle == null)
            {
                dropDownBoxStyle = new GUIStyle(EditorStyles.popup)
                {
                    fixedWidth = 150
                };
            }

            if (helpButtonStyle == null)
            {
                helpButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fixedWidth = 20
                };
            }
        }

        private void OnGUI()
        {
            SetStyles();

            LoadConfiguration();

            GUILayout.Space(5f);
            GUILayout.Label(windowName, titleStyle);
            GUILayout.Space(10f);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            slicerExportConfiguration.ExportPathPattern = EditorGUILayout.TextField(
                new GUIContent("Output Path Pattern", "The path that is used to build the output path for each exported mesh asset."),
                slicerExportConfiguration.ExportPathPattern);

            if (GUILayout.Button(new GUIContent("Reset", "Reset the Output Path Pattern"), resetPathStyle))
            {
                slicerExportConfiguration.ExportPathPattern = SlicerExportConfiguration.ExportPathPatternDefault;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();

            slicerExportConfiguration.RecursivelySearch = EditorGUILayout.ToggleLeft(
                new GUIContent("Recursively Search", "Should the exporter recursively search for sliced models to export."),
                slicerExportConfiguration.RecursivelySearch);

            slicerExportConfiguration.SaveProjectAfterExport = EditorGUILayout.ToggleLeft(
                new GUIContent("Save Project After Export", "Should the exporter save the project after it has completed exporting."),
                slicerExportConfiguration.SaveProjectAfterExport);

            slicerExportConfiguration.SaveAsNewAsset = EditorGUILayout.ToggleLeft(
                new GUIContent("Save As New Asset", "Should the exporter instantiate the mesh as a new asset when exporting."),
                slicerExportConfiguration.SaveAsNewAsset);

            EditorGUILayout.Separator();

            slicerExportConfiguration.FinalizeSlices = EditorGUILayout.ToggleLeft(
                new GUIContent("Finalize Slices", "Should the Slicer Controllers be finalized after it has completed exporting."),
                slicerExportConfiguration.FinalizeSlices);

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();

            slicerExportConfiguration.ExportSlicedMesh = EditorGUILayout.ToggleLeft(
                new GUIContent("Export Sliced Mesh", "Should meshes be exported."),
                slicerExportConfiguration.ExportSlicedMesh);

            slicerExportConfiguration.ExportSlicedColliders = EditorGUILayout.ToggleLeft(
                new GUIContent("Export Sliced Colliders", "Should colliders be exported."),
                slicerExportConfiguration.ExportSlicedColliders);

            EditorGUILayout.Separator();

            slicerExportConfiguration.OptimizeExportedMesh = EditorGUILayout.ToggleLeft(
                new GUIContent("Optimize Exported Mesh", "Should the exported mesh be optimized."),
                slicerExportConfiguration.OptimizeExportedMesh);

            slicerExportConfiguration.InheritMeshCompressionSettings = EditorGUILayout.ToggleLeft(
                new GUIContent("Inherit Mesh Compression Settings", "Should the Compression Settings from the original mesh be transfered to the exported mesh."),
                slicerExportConfiguration.InheritMeshCompressionSettings);

            EditorGUI.BeginDisabledGroup(slicerExportConfiguration.InheritMeshCompressionSettings);
            EditorGUI.indentLevel++;

            slicerExportConfiguration.ExportedMeshCompression = (ModelImporterMeshCompression)EditorGUILayout.EnumPopup(
                new GUIContent("Mesh Compression", "The compression setting for exported mesh."),
                slicerExportConfiguration.ExportedMeshCompression, dropDownBoxStyle);

            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Separator();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                UpdateFileList();
            }

            EditorGUILayout.LabelField("Export List:", EditorStyles.boldLabel);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            bool canExport = true;
            if (exportDetailsList.Count() == 0)
            {
                canExport = false;
                EditorGUILayout.LabelField("Note: No exportable sliced meshes are selected." +
                    "\n\nPlease select at least one Game Object or Open a Prefab that contains a Mesh or Collider Slicer Component.",
                    EditorStyles.wordWrappedLabel);
            }
            else
            {
                foreach (var exportDetails in exportDetailsList)
                {
                    var exportDetail = exportDetails.Value;
                    exportDetail.OnGUI(slicerExportConfiguration);

                    if (!exportDetail.skipExport && exportDetail.HasExportError)
                    {
                        canExport = false;
                    }
                }
            }

            if (!exportDetailsList.Any(e => !e.Value.skipExport))
            {
                canExport = false;
            }

            GUILayout.EndScrollView();

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(!canExport);
            if (GUILayout.Button(new GUIContent("Export As Mesh", "Export the listed mesh.")))
            {
                SaveConfigration();
                ExportSlicedMesh();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(new GUIContent("?", "Open the documentation."), helpButtonStyle))
            {
                Application.OpenURL(SlicerConfiguration.SiteUrl + SlicerConfiguration.ManualPath + "sliced_mesh_exporter.html");
            }

            EditorGUILayout.EndHorizontal();
        }

        private IEnumerable<ExportDetails> GetExportDetails(Transform t)
        {
            var slicerController = t.GetComponent<SlicerController>();
            var slicerComponents = t.GetComponents<SlicerComponent>();

            IEnumerable<ExportDetails> allExportDetails = null;
            if (slicerComponents != null && slicerComponents.Any())
            {
                allExportDetails = slicerComponents.SelectMany(e => BuildExportDetails(slicerController, e, slicerExportConfiguration.ExportPathPattern));
            }

            if (!slicerExportConfiguration.RecursivelySearch)
            {
                return allExportDetails;
            }

            foreach (Transform child in t)
            {
                var childExportDetails = GetExportDetails(child);

                if (childExportDetails != null)
                {
                    if (allExportDetails == null)
                    {
                        allExportDetails = childExportDetails;
                    }
                    else
                    {
                        allExportDetails = allExportDetails.Concat(childExportDetails);
                    }
                }
            }

            return allExportDetails;
        }

        private IEnumerable<ExportDetails> BuildExportDetails(SlicerController slicerController, SlicerComponent slicerComponent, string exportPathPattern)
        {
            if (slicerExportConfiguration.ExportSlicedMesh && slicerComponent is MeshSlicerComponent msc)
            {
                foreach (var md in msc.MeshDetailsList)
                {
                    if (md.SlicedMesh == null)
                    {
                        continue;
                    }

                    if (!exportDetailsList.TryGetValue(md.Id, out var exportDetails))
                    {
                        exportDetails = new ExportDetails
                        {
                            Id = md.Id,
                            SlicerController = slicerController,
                            SliceType = md.GetType(),
                            Mesh = md.SlicedMesh
                        };
                    }

                    exportDetails.AssetPath = BuildPath(msc, md, exportPathPattern);

                    yield return exportDetails;
                }
            }
            else if (slicerExportConfiguration.ExportSlicedColliders && slicerComponent is ColliderSlicerComponent csc)
            {
                foreach (var mcd in csc.MeshColliderDetailsList)
                {
                    if (mcd.SlicedMesh == null)
                    {
                        continue;
                    }

                    if (!exportDetailsList.TryGetValue(mcd.Id, out var exportDetails))
                    {
                        exportDetails = new ExportDetails
                        {
                            Id = mcd.Id,
                            SlicerController = slicerController,
                            SliceType = mcd.GetType(),
                            Mesh = mcd.SlicedMesh
                        };
                    }

                    exportDetails.AssetPath = BuildPath(csc, mcd, exportPathPattern);

                    yield return exportDetails;
                }
            }
        }

        private string BuildPath(SlicerComponent slicerComponent, string exportPathPattern)
        {
            var path = exportPathPattern;

            path = path.Replace("{ext}", "asset");
            path = path.Replace("{gameObjectName}", slicerComponent.gameObject.name);

            if (!path.StartsWith("Assets"))
            {
                path = $"Assets\\{path}";
            }

            return path;
        }

        private string BuildPath(MeshSlicerComponent slicerComponent, MeshDetails md, string exportPathPattern)
        {
            if (!slicerExportConfiguration.SaveAsNewAsset)
            {
                var assetCurrentPath = AssetDatabase.GetAssetPath(md.SlicedMesh);
                if (!string.IsNullOrEmpty(assetCurrentPath))
                {
                    return assetCurrentPath;
                }
            }

            var path = exportPathPattern;

            path = path.Replace("{meshName}", md.OriginalSharedMesh.name);
            path = path.Replace("{sliceType}", "slicedMesh");

            var meshAssetPath = AssetDatabase.GetAssetPath(md.OriginalSharedMesh);
            if (!string.IsNullOrEmpty(meshAssetPath))
            {
                path = path.Replace("{meshAssetFileName}", Path.GetFileNameWithoutExtension(meshAssetPath));
                path = path.Replace("{meshAssetFilePath}", Path.GetDirectoryName(meshAssetPath));
            }

            path = BuildPath(slicerComponent, path);

            return path;
        }

        private string BuildPath(ColliderSlicerComponent slicerComponent, MeshColliderDetails mcd, string exportPathPattern)
        {
            if (!slicerExportConfiguration.SaveAsNewAsset)
            {
                var assetCurrentPath = AssetDatabase.GetAssetPath(mcd.SlicedMesh);
                if (!string.IsNullOrEmpty(assetCurrentPath))
                {
                    return assetCurrentPath;
                }
            }

            var path = exportPathPattern;

            path = path.Replace("{meshName}", mcd.OriginalSharedMesh.name);
            path = path.Replace("{sliceType}", "slicedCollider");

            var meshAssetPath = AssetDatabase.GetAssetPath(mcd.OriginalSharedMesh);
            if (!string.IsNullOrEmpty(meshAssetPath))
            {
                path = path.Replace("{meshAssetFileName}", Path.GetFileNameWithoutExtension(meshAssetPath));
                path = path.Replace("{meshAssetFilePath}", Path.GetDirectoryName(meshAssetPath));
            }

            path = BuildPath(slicerComponent, path);

            return path;
        }

        private void ExportSlicedMesh()
        {
            foreach (var exportDetails in exportDetailsList)
            {
                var exportDetail = exportDetails.Value;

                if (exportDetail.skipExport)
                {
                    continue;
                }

                ExportMeshAsAsset(exportDetail.Mesh, exportDetail.AssetPath);
            }

            if (slicerExportConfiguration.SaveProjectAfterExport)
            {
                Debug.Log($"Saving assets post export.");
                AssetDatabase.SaveAssets();
            }

            if (slicerExportConfiguration.FinalizeSlices)
            {
                Debug.Log($"Finalizing Slices.");

                var distinctSlicerControllers = exportDetailsList
                    .Where(e => !e.Value.skipExport)
                    .Select(e => e.Value.SlicerController)
                    .Distinct()
                    .ToList();

                foreach (var distinctSlicerController in distinctSlicerControllers)
                {
                    distinctSlicerController.FinalizeSlicing();
                }
            }

            TempCollections.Clear();

            UpdateFileList();
        }

        private void ExportMeshAsAsset(Mesh mesh, string assetPath)
        {
            var folder = Path.GetDirectoryName(assetPath);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var assetTypeAtPath = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (assetTypeAtPath == typeof(Mesh))
            {
                var existingMeshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);

                Debug.Log($"Exporting {mesh.name} to '{assetPath}'. (Existing Export)");

                Core.MeshUtility.CopyMesh(mesh, existingMeshAsset);

                var meshCompression = slicerExportConfiguration.ExportedMeshCompression;
                if (slicerExportConfiguration.InheritMeshCompressionSettings)
                {
                    meshCompression = UnityEditor.MeshUtility.GetMeshCompression(mesh);
                }

                UnityEditor.MeshUtility.SetMeshCompression(existingMeshAsset, meshCompression);
                if (slicerExportConfiguration.OptimizeExportedMesh)
                {
                    UnityEditor.MeshUtility.Optimize(existingMeshAsset);
                }
            }
            else if (File.Exists(assetPath))
            {
                Debug.LogError($"Cannot export mesh to '{assetPath}'. The file type at the path is invalid.");
                return;
            }
            else
            {
                Mesh meshToSave = slicerExportConfiguration.SaveAsNewAsset ? GameObject.Instantiate(mesh) : mesh;
                Debug.Log($"Exporting {mesh.name} to '{assetPath}'. (New Export)");

                var meshCompression = UnityEditor.MeshUtility.GetMeshCompression(mesh);
                UnityEditor.MeshUtility.SetMeshCompression(meshToSave, meshCompression);

                if (slicerExportConfiguration.OptimizeExportedMesh)
                {
                    UnityEditor.MeshUtility.Optimize(meshToSave);
                }
                AssetDatabase.CreateAsset(meshToSave, assetPath);
            }
        }

        /// <summary>
        /// Loads the configuration.
        /// 
        /// If a custom configuration is being used the configuration file asset will be loaded and will overwrite the default configuration.
        /// </summary>
        private void LoadConfiguration()
        {
            if (slicerExportConfiguration != null)
            {
                // Already loaded
                return;
            }

            slicerExportConfiguration = CreateInstance<SlicerExportConfiguration>();
            slicerExportConfiguration.name = configurationFileName;

            var path = configurationFilePath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                EditorJsonUtility.FromJsonOverwrite(json, slicerExportConfiguration);
            }
        }

        private void SaveConfigration()
        {
            var path = configurationFilePath;
            if (!File.Exists(path))
            {
                var directory = Path.GetDirectoryName(path);
                Directory.CreateDirectory(directory);
            }

            try
            {
                File.WriteAllText(path, EditorJsonUtility.ToJson(slicerExportConfiguration, true));
            }
            catch (UnauthorizedAccessException)
            {
                Debug.LogWarning($"Could not save {configurationFileName} to {path}");
            }
        }
    }
    }

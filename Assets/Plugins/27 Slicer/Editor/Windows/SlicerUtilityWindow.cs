// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Slicer.Editor
{
    public class SlicerUtilityWindow : EditorWindow
    {
        private SerializedObject serializedCustomConfiguration;

        private Vector2 scrollPosition;

        [MenuItem("Window/27 Slicer/Configuration", false, 1200)]
        private static void Init()
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow<SlicerUtilityWindow>(true, "27 Slicer Configuration", true);
            window.minSize = new Vector2(400f, 600f);

            window.Show();

            SlicerConfiguration.LoadConfiguration();
        }

        [MenuItem("Window/27 Slicer/Open Documentation", false, 1290)]
        private static void OpenDocumentationSite()
        {
            Application.OpenURL(SlicerConfiguration.SiteUrl + SlicerConfiguration.ManualPath + "getting_started.html");
        }

        private void OnGUI()
        {
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 20,
            };

            GUILayout.Space(5f);
            GUILayout.Label("27 Slicer Configuration", titleStyle);
            GUILayout.Space(10f);

            if (Application.isPlaying)
            {
                PlayModeGui();
            }
            else
            {
                EditModeGui();
            }

        }

        private void OnDestroy()
        {
            if (serializedCustomConfiguration != null)
            {
                serializedCustomConfiguration.Dispose();
            }
        }

        private void PlayModeGui()
        {
            var playModeLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                wordWrap = true,
            };
            playModeLabelStyle.normal.textColor = Color.gray;

            GUILayout.Label("Configuration cannot be modified while in play mode.", playModeLabelStyle);
        }

        private void EditModeGui()
        {
            if (SlicerConfiguration.CustomConfigurationLoaded)
            {
                if (GUILayout.Button("Disable Custom Configuration"))
                {
                    DeleteConfigurationAsset();

                    if (serializedCustomConfiguration != null)
                    {
                        serializedCustomConfiguration.Dispose();
                        serializedCustomConfiguration = null;
                    }
                    SlicerConfiguration.LoadConfiguration();
                    return;
                }

                if (serializedCustomConfiguration == null)
                {
                    var configuration = Resources.Load<SlicerCustomConfiguration>(SlicerCustomConfiguration.FileName);
                    serializedCustomConfiguration = new SerializedObject(configuration);
                }
            }
            else
            {
                if (GUILayout.Button("Enable Custom Configuration"))
                {
                    CreateConfigurationAsset();

                    if (serializedCustomConfiguration != null)
                    {
                        serializedCustomConfiguration.Dispose();
                        serializedCustomConfiguration = null;
                    }

                    SlicerConfiguration.LoadConfiguration();
                    return;
                }

                if (serializedCustomConfiguration == null)
                {
                    var configuration = CreateInstance<SlicerCustomConfiguration>();
                    serializedCustomConfiguration = new SerializedObject(configuration);
                }

                GUI.enabled = false;
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            DrawConfiguration();

            GUILayout.EndScrollView();

            GUI.enabled = true;
        }

        private void DrawConfiguration()
        {
            EditorGUIUtility.labelWidth = 280;
            EditorGUIUtility.fieldWidth = 100;

            var serializedConfigurationProperty = serializedCustomConfiguration.GetIterator();
            serializedConfigurationProperty.Reset();
            while (serializedConfigurationProperty.NextVisible(true))
            {
                if (serializedConfigurationProperty.propertyType == SerializedPropertyType.ObjectReference ||
                    serializedConfigurationProperty.propertyType == SerializedPropertyType.ManagedReference)
                {
                    continue;
                }

                EditorGUILayout.PropertyField(serializedConfigurationProperty);
            }

            if (GUI.enabled)
            {
                var modificationsApplied = serializedCustomConfiguration.ApplyModifiedProperties();

                if (modificationsApplied)
                {
                    AssetDatabase.SaveAssets();
                    SlicerConfiguration.LoadConfiguration();
                }
            }
        }

        private void CreateConfigurationAsset()
        {
            var resourcesParentFolder = "Assets";
            var resourcesFolder = "Resources";
            var resourcesDirectory = $"{resourcesParentFolder}/{resourcesFolder}";
            var filePath = $"{resourcesDirectory}/{SlicerCustomConfiguration.FileFullName}";

            if (!Directory.Exists(resourcesDirectory))
            {
                AssetDatabase.CreateFolder(resourcesParentFolder, resourcesFolder);
            }

            AssetDatabase.CreateAsset(CreateInstance<SlicerCustomConfiguration>(), filePath);

            Debug.Log($"Created slicer configuration asset - '{filePath}'");
        }

        private void DeleteConfigurationAsset()
        {
            var resourcesParentFolder = "Assets";
            var resourcesFolder = "Resources";
            var resourcesDirectory = $"{resourcesParentFolder}/{resourcesFolder}";
            var filePath = $"{resourcesDirectory}/{SlicerCustomConfiguration.FileFullName}";
            var wasDeleted = AssetDatabase.DeleteAsset(filePath);

            if (wasDeleted)
            {
                Debug.Log($"Deleted slicer configuration asset - '{filePath}'");
            }
            else
            {
                Debug.LogWarning($"Could not delete slicer configuration asset - '{filePath}'");
            }

            if (!Directory.EnumerateFileSystemEntries(resourcesDirectory).Any())
            {
                Directory.Delete(resourcesDirectory);
                AssetDatabase.DeleteAsset($"{resourcesDirectory}.meta");

                Debug.Log($"Deleted empty resources folder - '{resourcesDirectory}'");
            }
        }
    }
}
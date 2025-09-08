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
    public class ExportDetails
    {
        public int Id { get; set; }
        public SlicerController SlicerController { get; set; }
        public Type SliceType { get; set; }
        public Mesh Mesh { get; set; }
        public string AssetPath { get; set; }

        private bool guiExpanded = false;

        public bool skipExport { get; set; }

        public bool HasExportError { get; private set; }

        private string GetSliceType()
        {
            if (SliceType == typeof(MeshDetails))
            {
                return "Mesh";
            }
            else if (SliceType == typeof(MeshColliderDetails))
            {
                return "Collider";
            }
            else
            {
                return "Unknown";
            }
        }

        private IEnumerable<GUIContent> BuildMessages(SlicerExportConfiguration slicerExportConfiguration)
        {
            HasExportError = false;

            var assetTypeAtPath = AssetDatabase.GetMainAssetTypeAtPath(AssetPath);

            if(assetTypeAtPath == typeof(Mesh))
            {
                var messageType = MessageType.Info;
                string msg = $"This Mesh has already been exported as an asset.";

                if (slicerExportConfiguration.SaveProjectAfterExport)
                {
                    msg += $"\nThis asset will be overwritten and saved during the export.";
                }
                else
                {
                    msg += $"\nThis asset will be overwritten during the export and must then be saved." +
                        $"\nSaving the Unity Project (File -> Save Project) will save this export." +
                        $"\nAlternatively you may enable 'Save Project After Export'.";
                    messageType = MessageType.Warning;
                }

                msg += $"\n\nAsset Location: {AssetPath}";

                yield return BuildGuiContent(msg, null, messageType);
            }
            else if (File.Exists(AssetPath))
            {
                var messageType = MessageType.Error;
                HasExportError = true;

                string msg = $"A file already exists at the export location and is not a Mesh asset." +
                    $"Please move this file to allow the export to continue." +
                    $"\n\nAsset Location: {AssetPath}";

                yield return BuildGuiContent(msg, null, messageType);
            }
        }

        /// <summary>
        /// Renders the GUI for this export
        /// </summary>
        /// <returns>Returns true is the export has errors</returns>
        public void OnGUI(SlicerExportConfiguration slicerExportConfiguration)
        {
            EditorGUI.indentLevel++;

            var origIconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(20, 20));

            var messages = BuildMessages(slicerExportConfiguration);

            EditorGUILayout.BeginHorizontal();

            var firstWarning = messages.FirstOrDefault();
            var exportHasWarning = firstWarning != null;
            var iconTexture = exportHasWarning ? firstWarning.image : null;
            var foldoutText = $"{GetSliceType()} - {AssetPath}";
            var foldoutStyle = EditorStyles.foldout;

            if (skipExport)
            {
                foldoutStyle = new GUIStyle(foldoutStyle);
                foldoutStyle.richText = true;
                foldoutText = $"<color=grey><i>{foldoutText}</i></color>";
            }

            var foldoutGuiContent = new GUIContent(foldoutText, iconTexture, null);
            guiExpanded = EditorGUILayout.Foldout(guiExpanded, foldoutGuiContent, foldoutStyle);

            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.SetIconSize(origIconSize);

            if (guiExpanded)
            {
                EditorGUI.indentLevel++;

                skipExport = EditorGUILayout.ToggleLeft("Skip Export", skipExport);

                MeshSlicerComponentEditor.ShowMeshDetails(Mesh, "Sliced Mesh", false);

                foreach (var msg in messages)
                {
                    EditorGUILayout.HelpBox(msg, true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        private GUIContent BuildGuiContent(string text, string tooltip = null, MessageType messageType = MessageType.None)
        {
            GUIContent guiContent;
            switch (messageType)
            {
                case MessageType.Info:
                    guiContent = EditorGUIUtility.IconContent("console.infoicon");
                    break;
                case MessageType.Warning:
                    guiContent = EditorGUIUtility.IconContent("console.warnicon");
                    break;
                case MessageType.Error:
                    guiContent = EditorGUIUtility.IconContent("console.erroricon");
                    break;
                default:
                    guiContent = new GUIContent();
                    break;
            }

            guiContent.text = text;
            guiContent.tooltip = tooltip;

            return guiContent;
        }
    }
}

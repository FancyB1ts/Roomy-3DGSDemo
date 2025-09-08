// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEditor;
using UnityEngine;

namespace Slicer.Core
{
    /// <summary>
    /// This <c>ScriptableObject</c> is used to save export configurations to file.
    /// </summary>
    public sealed class SlicerExportConfiguration : ScriptableObject
    {
        /// <summary>
        /// The default value of <see cref="ExportPathPattern"/>.
        /// </summary>
        public const string ExportPathPatternDefault = @"{meshAssetFilePath}\{meshAssetFileName}_{meshName}_{sliceType}.{ext}";
        /// <summary>
        /// The string pattern to use to determine output file of the exported mesh asset.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="ExportPathPatternDefault"/>.</value>
        public string ExportPathPattern = ExportPathPatternDefault;

        /// <summary>
        /// The default value of <see cref="RecursivelySearch"/>.
        /// </summary>
        public const bool RecursivelySearchDefault = false;
        /// <summary>
        /// Should the exporter recursively search for sliced models to export.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="RecursivelySearchDefault"/>.</value>
        public bool RecursivelySearch = RecursivelySearchDefault;

        /// <summary>
        /// The default value of <see cref="FinalizeSlices"/>.
        /// </summary>
        public const bool FinalizeSlicesDefault = false;
        /// <summary>
        /// Should the exporter recursively search for sliced models to export.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="FinalizeSlicesDefault"/>.</value>
        public bool FinalizeSlices = FinalizeSlicesDefault;

        /// <summary>
        /// The default value of <see cref="SaveProjectAfterExport"/>.
        /// </summary>
        public const bool SaveProjectAfterExportDefault = true;
        /// <summary>
        /// Should the exporter save the project after it has completed exporting.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="SaveProjectAfterExportDefault"/>.</value>
        public bool SaveProjectAfterExport = SaveProjectAfterExportDefault;

        /// <summary>
        /// The default value of <see cref="SaveAsNewAsset"/>.
        /// </summary>
        public const bool SaveAsNewAssetDefault = true;
        /// <summary>
        /// Should the exporter instantiate the mesh as a new asset when exporting.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="SaveAsNewAssetDefault"/>.</value>
        public bool SaveAsNewAsset = SaveAsNewAssetDefault;

        /// <summary>
        /// The default value of <see cref="ExportSlicedMesh"/>.
        /// </summary>
        public const bool ExportSlicedMeshDefault = true;
        /// <summary>
        /// Should meshes be exported.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="ExportSlicedMeshDefault"/>.</value>
        public bool ExportSlicedMesh = ExportSlicedMeshDefault;

        /// <summary>
        /// The default value of <see cref="ExportSlicedColliders"/>.
        /// </summary>
        public const bool ExportSlicedCollidersDefault = true;
        /// <summary>
        /// Should colliders be exported.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="ExportSlicedCollidersDefault"/>.</value>
        public bool ExportSlicedColliders = ExportSlicedCollidersDefault;

        /// <summary>
        /// The default value of <see cref="OptimizeExportedMesh"/>.
        /// </summary>
        public const bool OptimizeExportedMeshDefault = true;
        /// <summary>
        /// Should the exported mesh be optimized.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="OptimizeExportedMeshDefault"/>.</value>
        public bool OptimizeExportedMesh = OptimizeExportedMeshDefault;

        /// <summary>
        /// The default value of <see cref="InheritMeshCompressionSettings"/>.
        /// </summary>
        public const bool InheritMeshCompressionSettingsDefault = true;
        /// <summary>
        /// Should the Compression Settings from the original mesh be transfered to the exported mesh.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="InheritMeshCompressionSettingsDefault"/>.</value>
        public bool InheritMeshCompressionSettings = InheritMeshCompressionSettingsDefault;

        /// <summary>
        /// The default value of <see cref="ExportedMeshCompression"/>.
        /// </summary>
        public const ModelImporterMeshCompression ExportedMeshCompressionDefault = ModelImporterMeshCompression.Off;
        /// <summary>
        /// The compression setting for exported mesh.
        /// 
        /// This setting is active only when <see cref="InheritMeshCompressionSettings"/> is set to `false`
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="ExportedMeshCompressionDefault"/>.</value>
        public ModelImporterMeshCompression ExportedMeshCompression = ExportedMeshCompressionDefault;
    }
}
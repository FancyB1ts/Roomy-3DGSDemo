// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEngine;

namespace Slicer.Core
{
    /// <summary>
    /// This <c>ScriptableObject</c> is used to save custom configurations to file as an unity asset.
    /// </summary>
    public sealed class SlicerCustomConfiguration : ScriptableObject
    {
        /// <summary>
        /// The name of the configuration file.
        /// </summary>
        public const string FileName = "SlicerConfiguration";
        /// <summary>
        /// The name of the configuration file including the file extension.
        /// </summary>
        public const string FileFullName = FileName + ".asset";

        /// <summary>
        /// The default value of <see cref="SkipUnmodifiedSlices"/>.
        /// </summary>
        public const bool SkipUnmodifiedSlicesDefault = true;
        /// <summary>
        /// Optimization - When enabled the slicer will only update if the slicer has been modified.
        /// It is recommended to leave this enabled but is useful for debugging.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="SkipUnmodifiedSlicesDefault"/>.</value>
        [Tooltip("Optimization - When enabled the slicer will only update if the slicer has been modified.\n\nIt is recommended to leave this enabled but is useful for debugging.")]
        [Header("Optimizations")]
        public bool SkipUnmodifiedSlices = SkipUnmodifiedSlicesDefault;

        /// <summary>
        /// The default value of <see cref="RefreshSlicesOnUpdate"/>.
        /// </summary>
        public const bool RefreshSlicesOnUpdateDefault = false;
        /// <summary>
        /// Optimization - When enabled the slicer will update every frame.
        /// It is recommended to leave this disabled and call <c>SlicerController.RefreshSlice()</c> when an update is required.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="RefreshSlicesOnUpdateDefault"/>.</value>
        [Tooltip("Optimization - When enabled the slicer will update every frame.\n\nIt is recommended to leave this disabled and call 'SlicerController.RefreshSlice()' when an update is required.")]
        public bool RefreshSlicesOnUpdate = RefreshSlicesOnUpdateDefault;



        /// <summary>
        /// The default value of <see cref="FinalizeOnStart"/>.
        /// </summary>
        public const bool FinalizeOnStartDefault = false;
        /// <summary>
        /// When enabled all slicers will be finalized immediately after they start at runtime.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="FinalizeOnStartDefault"/>.</value>
        [Tooltip("When enabled all slicers will be finalized immediately after they start at runtime.")]
        [Header("Sliced Mesh Finalization")]
        public bool FinalizeOnStart = FinalizeOnStartDefault;

        /// <summary>
        /// The default value of <see cref="UseFinalizedSlicedMeshComponent"/>.
        /// </summary>
        public const bool UseFinalizedSlicedMeshComponentDefault = true;
        /// <summary>
        /// When finalizing sliced meshes also initialize a FinalizedSlicedMesh component that will manage the lifecycle of the sliced mesh.
        /// 
        /// e.g. The sliced mesh will be destroyed when the `FinalizedSlicedMesh` component is destroyed.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="UseFinalizedSlicedMeshComponentDefault"/>.</value>
        [Tooltip("When finalizing sliced meshes also initialize a FinalizedSlicedMesh component that will manage the lifecycle of the sliced mesh.")]
        public bool UseFinalizedSlicedMeshComponent = UseFinalizedSlicedMeshComponentDefault;

        /// <summary>
        /// The default value of <see cref="SetFinalizedSlicedMeshToBeNoLongerReadable"/>.
        /// </summary>
        public const bool SetFinalizedSlicedMeshToBeNoLongerReadableDefault = true;
        /// <summary>
        /// When finalizing a sliced mesh also set it to be no longer readable.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="SetFinalizedSlicedMeshToBeNoLongerReadableDefault"/>.</value>
        [Tooltip("When finalizing a sliced mesh also set it to be no longer readable.")]
        public bool SetFinalizedSlicedMeshToBeNoLongerReadable = SetFinalizedSlicedMeshToBeNoLongerReadableDefault;

        /// <summary>
        /// The default value of <see cref="OptimizeFinalizedSlicedMesh"/>.
        /// </summary>
        public const bool OptimizeFinalizedSlicedMeshDefault = true;
        /// <summary>
        /// When finalizing a sliced mesh also Optimize it.
        /// </summary>
        /// <value>The default value for this setting is set by <see cref="OptimizeFinalizedSlicedMeshDefault"/>.</value>
        [Tooltip("When finalizing a sliced mesh also Optimize it.")]
        public bool OptimizeFinalizedSlicedMesh = OptimizeFinalizedSlicedMeshDefault;
    }
}
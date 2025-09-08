// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEngine;

namespace Slicer.Core
{
    /// <summary>
    /// Static class used for managing configuration and other useful information for the slicer.
    /// </summary>
    /// <remarks>
    /// > [!Warning]
    /// > The configuration should not be changed during runtime and instead should only be modified using the "27 Slicer Configuration" window.
    /// > This window can be found by navigating to Window -> 27 Slicer -> Configuration in the Unity Editor.
    /// </remarks>
    public static class SlicerConfiguration
    {
        static SlicerConfiguration()
        {
            LoadConfiguration();
        }

        /// <summary>
        /// The URL to the 27 Slicer web site.
        /// </summary>
        public const string SiteUrl = "https://slicer.deftly.games";

        /// <summary>
        /// The path to the reference manuals for components on the web site.
        /// </summary>
        public const string ComponentsManualPath = "/manual/components/";

        /// <summary>
        /// The path to the reference manuals.
        /// </summary>
        public const string ManualPath = "/manual/";

        /// <summary>
        /// Gets a value indicating whether the configuration has been customized and loaded from file.
        /// </summary>
        /// <value>
        ///   <c>true</c> if a custom configuration is being used; otherwise, <c>false</c>.
        /// </value>
        public static bool CustomConfigurationLoaded { get; private set; } = false;

        /// <inheritdoc cref="SlicerCustomConfiguration.SkipUnmodifiedSlices"/>
        public static bool SkipUnmodifiedSlices { get; private set; }

        /// <inheritdoc cref="SlicerCustomConfiguration.RefreshSlicesOnUpdate"/>
        public static bool RefreshSlicesOnUpdate { get; private set; }

        /// <inheritdoc cref="SlicerCustomConfiguration.FinalizeOnStart"/>
        public static bool FinalizeOnStart { get; private set; }

        /// <inheritdoc cref="SlicerCustomConfiguration.UseFinalizedSlicedMeshComponent"/>
        public static bool UseFinalizedSlicedMeshComponent { get; private set; }

        /// <inheritdoc cref="SlicerCustomConfiguration.SetFinalizedSlicedMeshToBeNoLongerReadable"/>
        public static bool SetFinalizedSlicedMeshToBeNoLongerReadable { get; private set; }

        /// <inheritdoc cref="SlicerCustomConfiguration.OptimizeFinalizedSlicedMesh"/>
        public static bool OptimizeFinalizedSlicedMesh { get; private set; }

        /// <summary>
        /// Loads the configuration.
        /// 
        /// If a custom configuration is being used the configuration file asset will be loaded and will overwrite the default configuration.
        /// </summary>
        public static void LoadConfiguration()
        {
            var configurationFile = Resources.Load<SlicerCustomConfiguration>(SlicerCustomConfiguration.FileName);

            if (configurationFile != null)
            {
                LoadCustomConfiguration(configurationFile);

                Resources.UnloadAsset(configurationFile);
            }
            else
            {
                LoadDefaultConfiguration();
            }
        }

        private static void LoadCustomConfiguration(SlicerCustomConfiguration configurationFile)
        {
            CustomConfigurationLoaded = true;

            SkipUnmodifiedSlices = configurationFile.SkipUnmodifiedSlices;
            RefreshSlicesOnUpdate = configurationFile.RefreshSlicesOnUpdate;
            FinalizeOnStart = configurationFile.FinalizeOnStart;
            UseFinalizedSlicedMeshComponent = configurationFile.UseFinalizedSlicedMeshComponent;
            SetFinalizedSlicedMeshToBeNoLongerReadable = configurationFile.SetFinalizedSlicedMeshToBeNoLongerReadable;
            OptimizeFinalizedSlicedMesh = configurationFile.OptimizeFinalizedSlicedMesh;
        }

        private static void LoadDefaultConfiguration()
        {
            CustomConfigurationLoaded = false;

            SkipUnmodifiedSlices = SlicerCustomConfiguration.SkipUnmodifiedSlicesDefault;
            RefreshSlicesOnUpdate = SlicerCustomConfiguration.RefreshSlicesOnUpdateDefault;
            FinalizeOnStart = SlicerCustomConfiguration.FinalizeOnStartDefault;
            UseFinalizedSlicedMeshComponent = SlicerCustomConfiguration.UseFinalizedSlicedMeshComponentDefault;
            SetFinalizedSlicedMeshToBeNoLongerReadable = SlicerCustomConfiguration.SetFinalizedSlicedMeshToBeNoLongerReadableDefault;
            OptimizeFinalizedSlicedMesh = SlicerCustomConfiguration.OptimizeFinalizedSlicedMeshDefault;
        }

        public new static string ToString()
        {
            var str = $"27 Slicer Configuration\n" +
                $"CustomConfigurationLoaded = {CustomConfigurationLoaded}\n" +
                $"\n" +
                $"SkipUnmodifiedSlices = {SkipUnmodifiedSlices}\n" +
                $"RefreshSlicesOnUpdate = {RefreshSlicesOnUpdate}\n" +
                $"FinalizeOnStart = {FinalizeOnStart}\n" +
                $"UseFinalizedSlicedMeshComponent = {UseFinalizedSlicedMeshComponent}\n" +
                $"SetFinalizedSlicedMeshToBeNoLongerReadable = {SetFinalizedSlicedMeshToBeNoLongerReadable}\n" +
                $"OptimizeFinalizedSlicedMesh = {OptimizeFinalizedSlicedMesh}\n";

            return str;
        }
    }
}
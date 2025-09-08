// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// Used to track Sliced Meshes that was created by 27 Slicer and then Finalized.
    /// 
    /// This component will destroy the mesh assigned to the <see cref="SlicedMesh"/> property when this component is destroyed. This is intended to manage the lifecycle of the mesh and prevents memory leaks. 
    /// </summary>
    public class FinalizedSlicedMesh : MonoBehaviour
    {
        /// <summary>
        /// The mesh that is being tracked by this component.
        /// </summary>
        public Mesh SlicedMesh;

        private void OnDestroy()
        {
            if (SlicedMesh != null)
            {
                Destroy(SlicedMesh);
            }
        }
    }
}
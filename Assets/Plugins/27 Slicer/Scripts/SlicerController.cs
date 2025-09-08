// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// SlicerControllers are used to manage the slicing of <see cref="SlicerComponent" />s that are on the same GameObject as itself.
    /// </summary>
    /// <example>
    /// In the below image the SlicerController is managing the <see cref="MeshSlicerComponent"/> which will slice any Meshes that are a decedent of this GameObject.
    /// 
    /// The <see cref="Size"/> property is set to double the <span style="color:rgb(255,0,0)">x</span> dimension of the mesh while keeping the other dimensions the same as the original mesh.
    /// 
    /// The <see cref="Slices"/> property defines the where the slices should occur. Ranging from 0 (the center of the object), to 1 being the furthest extents of the object.
    /// 
    /// <a href="/images/inspector_slicer_controller_mesh_slicer_component.png" target="_blank">
    ///     <img src="/images/inspector_slicer_controller_mesh_slicer_component.png" />
    /// </a>
    /// 
    /// <br /><br />[REFERENCE MANUAL](xref:manual\components\slicer_controller)
    /// </example>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [AddComponentMenu("Slicer/Slicer Controller")]
    [HelpURL(SlicerConfiguration.SiteUrl + SlicerConfiguration.ComponentsManualPath + "slicer_controller.html")]
    public class SlicerController : MonoBehaviour
    {
        /// <summary>
        /// Adjusts (Scales) the final dimensions of the sliced item.
        /// </summary>
        /// <example>
        /// Setting the vector to (<span style="color:rgb(255,0,0)">x</span>: 2, <span style="color:rgb(127,255,0)">y</span>: 1.5, <span style="color:rgb(0,127,255)">z</span>: 0.5)
        /// will double the <span style="color:rgb(255,0,0)">x</span> dimension, add an additional 50% of the size to the <span style="color:rgb(127,255,0)">y</span> dimension and will halve the <span style="color:rgb(0,127,255)">z</span> dimension.
        /// </example>
        [Tooltip("Adjusts (Scales) the final dimensions of the sliced item.")]
        public Vector3 Size = Vector3.one;

        /// <summary>
        /// Adjusts where the slices should occur. Ranging from 0 (the center of the object), to 1 being the furthest extents of the object.
        /// </summary>
        /// <example>
        /// In the case of Mesh getting sliced, any vertices that fall within between the center of the Mesh and the provided values will be stretched (scaled) by the value of the Size property.
        /// Any vertices that are greater than the provided value will be moved (translated) linearly.
        /// </example>
        [Tooltip("Adjusts where the slices should occur.\nRanging from 0 (the center of the object),\nto 1 being the furthest extents of the object.")]
        public Vector3 Slices = Vector3.one;

        /// <summary>
        /// Offsets the <c>SlicedBounds</c>, useful if the sliced items needs to be offset without affecting the where it falls in the slice.
        /// </summary>
        [HideInInspector]
        public Vector3 Offset = Vector3.zero;

        private List<SlicerComponent> slicerComponents;
        private List<SlicerComponent> previousSlicerComponents;
        /// <summary>
        /// A read only collection of <see cref="SlicerComponent">SlicerComponents</see>
        /// </summary>
        public ReadOnlyCollection<SlicerComponent> SlicerComponents { get { return slicerComponents?.AsReadOnly(); } }

        private List<SliceModifier> sliceModifiers;
        private List<SliceModifier> previousSliceModifiers;
        /// <summary>
        /// A read only collection of <see cref="SliceModifiers">SliceModifiers</see>
        /// </summary>
        public ReadOnlyCollection<SliceModifier> SliceModifiers { get { return sliceModifiers?.AsReadOnly(); } }

        private List<SlicerIgnore> slicerIgnores;
        /// <summary>
        /// A read only collection of <see cref="SlicerIgnore">SlicerIgnores</see>
        /// </summary>
        public ReadOnlyCollection<SlicerIgnore> SlicerIgnores { get { return slicerIgnores?.AsReadOnly(); } }

        // Used by non-alloc GetComponents()
        private static readonly List<SliceModifier> childSliceModifiers = new List<SliceModifier>();

        /// <summary>
        /// The bounding box of all of the of all the items that are being sliced.
        /// </summary>
        /// <remarks>
        /// It is in Local Object Space (of the GameObject the SlicerController Component is attached to).
        /// 
        /// The bounds will be null if it has not been calculated yet.
        /// </remarks>
        public Bounds? CompleteBounds { get; private set; }

        /// <summary>
        /// The dimensions that will be used by the slicers to determine if particular features are to be stretched (scaled).
        /// </summary>
        /// <remarks>
        /// They will be stretched (scaled) if the point is contained by this bounding box and will moved (translated) linearly if they fall outside of the box.
        /// 
        /// It is in Local Object Space (of the GameObject the SlicerController Component is attached to).
        /// </remarks>
        public Bounds SlicedBounds { get; private set; }

        [SerializeField, HideInInspector]
        private Hash128 previousHash;

        /// <summary>
        /// The hash that was calculated during the previous slice
        /// </summary>
        public Hash128 PreviousHash { get { return previousHash; } }

        private bool forceSliceUpdate = true;

        private void Start()
        {
            UpdateSlice();

            if (SlicerConfiguration.FinalizeOnStart)
            {
                if (Application.isPlaying)
                {
                    // We never want this to run outside of play mode
                    FinalizeSlicing(true);
                }
            }
        }

        private void Update()
        {
            if (ShouldUpdateSlice())
            {
                UpdateSlice();
            }
        }

        private bool ShouldUpdateSlice()
        {
#if UNITY_EDITOR // If we are not in the editor, then we are always playing
            if (!Application.isPlaying)
            {
                return true;
            }
#endif

            if (SlicerConfiguration.RefreshSlicesOnUpdate)
            {
                // Configuration has been set to update on every frame
                return true;
            }

            if (forceSliceUpdate)
            {
                // We have been requested to force a slice update
                return true;
            }


            return false;
        }

        /// <summary>
        /// Refreshes the slices managed by this controller on the next frame update.
        /// </summary>
        /// <remarks>
        /// It is recommended to only call this when there have been changes made during runtime that requires recalculation of the slices.
        /// </remarks>
        public void RefreshSlice()
        {
            forceSliceUpdate = true;
        }

        /// <summary>
        /// Refreshes the slices managed by this controller, this slice happens immediately.
        /// 
        /// Generally you will want to use <see cref="RefreshSlice"/> instead.
        /// </summary>
        /// <remarks>
        /// It is recommended to only call this when there have been changes made during runtime that requires recalculation of the slices.
        /// </remarks>
        public void RefreshSliceImmediate()
        {
            UpdateSlice();
        }

        private void UpdateSlice()
        {
            forceSliceUpdate = false; // Reset the forced update, as we are doing it now

            var currentHash = GatherDetails();

            var sizeHash = HashUtility.CalculateHash(Size);
            var boundsHash = HashUtility.CalculateHash(Slices);
            HashUtility.AppendHash(sizeHash, boundsHash, ref currentHash);

            if (previousHash != currentHash || !SlicerConfiguration.SkipUnmodifiedSlices)
            {
                Slice();

                Modify();

                previousHash = currentHash;
            }
        }

        private Hash128 GatherDetails()
        {
            // Rotate the Slicer Component collection,
            // We will use these two collections to determine if a slicer component has been removed
            if (slicerComponents == null)
            {
                slicerComponents = new List<SlicerComponent>();
                previousSlicerComponents = new List<SlicerComponent>();
            }
            else
            {
                var tempSlicerComponents = previousSlicerComponents;
                previousSlicerComponents = slicerComponents;
                slicerComponents = tempSlicerComponents;
            }

            if (sliceModifiers == null)
            {
                sliceModifiers = new List<SliceModifier>();
                previousSliceModifiers = new List<SliceModifier>();
            }
            else
            {
                var tempSliceModifiers = previousSliceModifiers;
                previousSliceModifiers = sliceModifiers;
                sliceModifiers = tempSliceModifiers;
                sliceModifiers.Clear();
            }

            if (slicerIgnores == null)
            {
                slicerIgnores = new List<SlicerIgnore>();
            }
            else
            {
                slicerIgnores.Clear();
            }

            for (int i = slicerComponents.Count - 1; i >= 0; i--)
            {
                if (slicerComponents[i] == null)
                {
                    slicerComponents.RemoveAt(i);
                }
            }

            GetComponents(slicerComponents);

            // If this slicer component is removed, disable the slicer so it sets all of the items back to their default
            foreach (var previousSlicerComponent in previousSlicerComponents)
            {
                if (!slicerComponents.Contains(previousSlicerComponent))
                {
                    previousSlicerComponent.DisableSlicing();
                }
            }

            {
                var lastElement = slicerComponents.Count - 1;
                for (int i = lastElement; i >= 0; i--)
                {
                    SlicerComponent slicerComponent = slicerComponents[i];
                    if (slicerComponent == null)
                    {
                        slicerComponents.RemoveAt(i);
                        lastElement--;
                    }

                    slicerComponent.PreGatherDetails();

                    // Some slicer components should be run after others
                    // So we want to make sure that are at the back
                    if (slicerComponent is ColliderSlicerComponent)
                    {
                        if (i != lastElement)
                        {
                            slicerComponents[i] = slicerComponents[lastElement];
                            slicerComponents[lastElement] = slicerComponent;
                        }
                        lastElement--;
                    }
                }
            }

            GetChildDetails(transform);

            CalculateBounds();

            var hash = new Hash128();
            foreach (var slicerComponent in slicerComponents)
            {
                var tempHash = slicerComponent.PostGatherDetails();
                HashUtility.AppendHash(tempHash, ref hash);
            }

            foreach (var sliceModifier in sliceModifiers)
            {
                var tempHash = sliceModifier.GatherDetails();
                HashUtility.AppendHash(tempHash, ref hash);
            }

            // If this slice modifier is removed, disable the slicer so it sets all of the items back to their default
            foreach (var previousSliceModifier in previousSliceModifiers)
            {
                if (previousSliceModifier != null && !sliceModifiers.Contains(previousSliceModifier))
                {
                    previousSliceModifier.DisableModifier();
                }
            }

            previousSlicerComponents.Clear();
            previousSliceModifiers.Clear();

            return hash;
        }

        private void GetChildDetails(Transform currentTransform)
        {
            foreach (Transform childTransform in currentTransform)
            {
                // If we encounter another SlicerController:
                //      The found controller manages any sibling and decedent SlicerComponents
                //      This controller manages the sibling SliceModifiers of the found controller
                //      The found controller manages decedent SliceModifiers
                // If we encounter a SlicerIgnore:
                //      This controller manages any sibling SliceModifiers but stop navigating its decedents

                childTransform.GetComponents(childSliceModifiers);
                if (childSliceModifiers.Count > 0)
                {
                    // Add the slice modifiers
                    sliceModifiers.AddRange(childSliceModifiers);
                    childSliceModifiers.Clear();
                }

                var childSlicer = childTransform.GetComponent<SlicerController>();
                if (childSlicer != null)
                {
                    // Don't mess with other slicer controllers
                    continue;
                }

                var childSlicerIgnore = childTransform.GetComponent<SlicerIgnore>();
                if (childSlicerIgnore != null)
                {
                    // Don't continue with GameObjects that have a SlicerIgnore
                    slicerIgnores.Add(childSlicerIgnore);
                    continue;
                }

                foreach (var slicerComponent in slicerComponents)
                {
                    slicerComponent.GatherDetails(childTransform, transform);
                }

                GetChildDetails(childTransform);
            }
        }

        private void CalculateBounds()
        {
            CompleteBounds = null;

            foreach (var slicerComponent in slicerComponents)
            {
                var componentBounds = slicerComponent.CalculateBounds();
                CompleteBounds = BoundsUtility.Encapsulate(CompleteBounds, componentBounds);
            }

            if (!CompleteBounds.HasValue)
            {
                // There are no bounds set
                // We have no data to work with

                return;
            }

            SlicedBounds = CalculateSlicedBounds(CompleteBounds.Value);
        }

        private void Slice()
        {
            if (!CompleteBounds.HasValue)
            {
                // There are no bounds set
                // We have no data to work with

                return;
            }

            foreach (var slicerComponent in slicerComponents)
            {
                if (!slicerComponent.isActiveAndEnabled || !slicerComponent.SlicingEnabled)
                {
                    continue;
                }

                slicerComponent.Slice(Size, transform, CompleteBounds.Value, SlicedBounds, Slices);
            }
        }

        private void Modify()
        {
            if (!CompleteBounds.HasValue)
            {
                // There are no bounds set
                // We have no data to work with

                return;
            }

            foreach (var sliceModifier in sliceModifiers)
            {
                if (!sliceModifier.isActiveAndEnabled)
                {
                    continue;
                }

                sliceModifier.Modify(Size, transform, CompleteBounds.Value, SlicedBounds);
            }
        }

        /// <summary>
        /// Disables slicing for all of the SlicerComponents controlled by this SlicerController.
        /// </summary>
        public void DisableSlicing()
        {
            foreach (var slicerComponent in slicerComponents)
            {
                slicerComponent.DisableSlicing();
            }

            foreach (var sliceModifier in sliceModifiers)
            {
                sliceModifier.DisableModifier();
            }
        }

        /// <summary>
        /// Enables slicing for all of the SlicerComponents controlled by this SlicerController.
        /// </summary>
        public void EnableSlicing()
        {
            foreach (var slicerComponent in slicerComponents)
            {
                slicerComponent.EnableSlicing();
            }

            foreach (var sliceModifier in sliceModifiers)
            {
                sliceModifier.EnableModifier();
            }
        }

        /// <summary>
        /// Finalizes slicing for this Slicer Controller.
        /// </summary>
        /// <param name="deferDestroy">Wait until the end of the frame to destroy this controller.</param>
        public void FinalizeSlicing(bool deferDestroy = false)
        {
            foreach (var slicerComponent in slicerComponents)
            {
                slicerComponent.FinalizeSlicing();
            }
            slicerComponents.Clear();

            foreach (var sliceModifier in sliceModifiers)
            {
                sliceModifier.FinalizeSlicing();
            }
            sliceModifiers.Clear();

            foreach (var slicerIgnore in slicerIgnores)
            {
                slicerIgnore.FinalizeSlicing();
            }
            slicerIgnores.Clear();

            if (deferDestroy)
            {
                this.StartCoroutine(LateSafeDestroy(this));
            }
            else
            {
                SafeDestroy(this);
            }
        }

        private Bounds CalculateSlicedBounds(Bounds defaultBounds)
        {
            var result = defaultBounds;

            var extents = result.extents;
            extents.Scale(Slices);
            result.extents = extents;

            result.center += Offset;

            return result;
        }

        /// <summary>
        /// When in <see cref="Application.isPlaying"/> is true do a <see cref="Object.Destroy"/> otherwise do <see cref="Object.DestroyImmediate"/>.
        /// </summary>
        internal static void SafeDestroy(Object toDestroy)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Object.Destroy(toDestroy);
            }
            else
            {
                Object.DestroyImmediate(toDestroy, false);
            }
#else
            GameObject.Destroy(toDestroy);
#endif
        }

        internal static IEnumerator LateSafeDestroy(Object toDestroy)
        {
            yield return new WaitForEndOfFrame();

            SafeDestroy(toDestroy);
        }
    }
}
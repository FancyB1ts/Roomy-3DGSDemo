// 27 Slicer
// Copyright 2021 Deftly Games
// https://slicer.deftly.games/

using Slicer.Core;
using UnityEngine;

namespace Slicer
{
    /// <summary>
    /// When applied to a GameObject, SlicerIgnore will block any processing of it's decedents. Any <see cref="SliceModifier"/>s that are on the same GameObject as the SlicerIgnore will be processed as normal.
    /// </summary>
    /// <example>
    /// Consider the following GameObject hierarchy. The mug and tablecloth both have a SlicerIgnore. Its decedents (Mug Model and Tablecloth Model) will not be sliced.
    /// 
    /// However, <see cref="SliceModifier"/>s that are on the same GameObject as the SlicerIgnore will be processed as normal.
    /// 
    /// [!include[Inclusion Simple Hierarchy](~/apidoc/inclusion_simple_hierarchy.md)]
    /// 
    /// <br /><br />[REFERENCE MANUAL](xref:manual\components\slicer_ignore)
    /// </example>
    [AddComponentMenu("Slicer/Slicer Ignore")]
    [HelpURL(SlicerConfiguration.SiteUrl + SlicerConfiguration.ComponentsManualPath + "slicer_ignore.html")]
    public class SlicerIgnore : MonoBehaviour
    {
        /// <summary>
        /// Finalizes slicing for this Slicer Ignore.
        /// </summary>
        public void FinalizeSlicing()
        {
            SlicerController.SafeDestroy(this);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace PhotonSail
{
    [KSPAddon(KSPAddon.Startup.EditorVAB, false)]
    class PhotonSailFilter : MonoBehaviour
    {
        void Start()
        {
            EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Photon Sailor Hidden Filter", p => p.TechRequired != "hidden"));
            EditorPartList.Instance.Refresh();
        }
    }
}

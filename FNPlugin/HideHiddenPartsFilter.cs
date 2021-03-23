using UnityEngine;
using KSP.UI.Screens;

namespace FNPlugin
{
    [KSPAddon(KSPAddon.Startup.EditorVAB, false)]
    class HideHiddenPartsFilter : MonoBehaviour
    {
        void Start()
        {
            EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("WarpPlugin Parts Filter", p => p.TechRequired != "hidden"));
            EditorPartList.Instance.Refresh();
        }
    }
}

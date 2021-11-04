using System.Linq;
using Waterfall;

namespace FNPlugin.Propulsion
{
    // This class exists solely as a shield for referencing Waterfall, so it can be referenced without spamming errors if it isn't installed.
    // This should be the only class referencing Waterfall, and you should ALWAYS verify that a ModuleWaterfallFX is present before calling
    // anything in this class -- a quick way to do this is if (part.Modules.Contains("ModuleWaterfallFX")) 
    public class WaterfallIntegration
    {
        public static void RCSPower(Part part, string thrusterTransform, bool rcsPowered)
        {
            foreach (ModuleWaterfallFX module in part.FindModulesImplementing<ModuleWaterfallFX>())
                // HeavyRCS has two effects that are independently powered, this distinguishes them by their transform
                if(module.FX.First().parentName == thrusterTransform)
                    foreach (WaterfallController controller in module.Controllers)                    
                        if (controller.GetType() == typeof(CustomController) && controller.name == "rcsPower")
                        {
                            // for some reason controller.SetOverride(true) doesn't work
                            // this also takes over the override from the effect editor UI,
                            // so you'll have to manually alter whatever drives your controller
                            controller.overridden = true;
                            if (rcsPowered) controller.SetOverrideValue(1);
                            else controller.SetOverrideValue(0);                        
                        }            
        }
    }
}

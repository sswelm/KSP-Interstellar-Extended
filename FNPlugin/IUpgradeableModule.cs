using System;

namespace FNPlugin 
{
    public interface IUpgradeableModule 
    {
        String UpgradeTechnology { get; }
        void upgradePartModule();
    }

    public static class UpgradeableModuleExtensions 
    {
        public static bool HasTechsRequiredToUpgrade(this IUpgradeableModule upg_module)
        {
            return PluginHelper.UpgradeAvailable(upg_module.UpgradeTechnology);
        }
    }
}


namespace FNPlugin.Refinery
{
	[KSPModule("Power Supply")]
    class InterstellarPowerSupply : ResourceSuppliableModule, IPowerSupply
	{
		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Proces")]
		public string displayName = "";

		public string DisplayName
		{
			get { return displayName; }
			set { displayName = value; }
		}

		public override void OnStart(PartModule.StartState state)
		{
			displayName = part.partInfo.title;
		}

		public double ConsumeMegajoulesFixed(double powerRequest, double fixedDeltaTime)
		{
			return consumeFNResource(powerRequest, ResourceManager.FNRESOURCE_MEGAJOULES, fixedDeltaTime);
		}

		public double ConsumeMegajoulesPerSecond(double powerRequest)
		{
            return consumeFNResourcePerSecond(powerRequest, ResourceManager.FNRESOURCE_MEGAJOULES);
		}

		public override string getResourceManagerDisplayName()
		{
			return displayName;
		}

		public override string GetInfo()
		{
			return displayName;
		}

		public override int getPowerPriority()
		{
			return 4;
		}
	}
}

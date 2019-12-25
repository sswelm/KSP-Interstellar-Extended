using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Storage
{
    [KSPModule("Kerbalism Habitat Controller")]
	class KerbalismHabitatController : PartModule
	{
		PartModule habitatModule;
		BaseField habitatVolumeField;
		BaseField habitatSurfaceField;

		[KSPField]bool isInitialized;

		public double Volume
		{
			get 
			{
				if (isInitialized == false)
					InitializeKerbalismHabitat();

				if (habitatVolumeField == null)
					return -1;

				return (double)habitatVolumeField.GetValue(habitatModule);
			}
			set
			{
				if (isInitialized == false)
					InitializeKerbalismHabitat();

				if (habitatVolumeField == null)
					return;

				habitatVolumeField.SetValue(value, habitatModule);
			}
		}

		public double Surface
		{
			get
			{
				if (isInitialized == false)
					InitializeKerbalismHabitat();

				if (habitatSurfaceField == null)
					return -1;

				return (double)habitatSurfaceField.GetValue(habitatModule);
			}
			set
			{
				if (isInitialized == false)
					InitializeKerbalismHabitat();

				if (habitatSurfaceField == null)
					return;

				habitatSurfaceField.SetValue(value, habitatModule);
			}
		}

		public override void OnStart(StartState state)
		{
            UnityEngine.Debug.Log("[KSPI]: KerbalismHabitatController start on " + part.partInfo.title);
			InitializeKerbalismHabitat();
		}

		private void InitializeKerbalismHabitat()
		{
			isInitialized = true;

			bool found = false;

			foreach (PartModule module in part.Modules)
			{
				if (module.moduleName == "Habitat")
				{
					habitatModule = module;
					found = true;

					habitatVolumeField = module.Fields["volume"];

					if (habitatVolumeField == null)
						UnityEngine.Debug.LogError("[KSPI]: volume Field not found on Habitat");

					habitatSurfaceField = module.Fields["surface"];

					if (habitatSurfaceField == null)
						UnityEngine.Debug.LogError("[KSPI]: surface Field not found on Habitat");

					break;
				}
			}

            //if (found)
            //    UnityEngine.Debug.Log("[KSPI]: Found Habitat PartModule on " + part.partInfo.title );
            //else
            //    UnityEngine.Debug.LogWarning("[KSPI]: No Habitat PartModule found on " + part.partInfo.title);
		}
	}
}
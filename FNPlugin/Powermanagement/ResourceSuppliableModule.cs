using System;
using System.Collections.Generic;
using System.Linq;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using UnityEngine;

namespace FNPlugin.Powermanagement
{
    public abstract class ResourceSuppliableModule : PartModule, IResourceSuppliable, IResourceSupplier
    {
        [KSPField(isPersistant = true)] public int epx;
        [KSPField(isPersistant = true)] public int epy;
        [KSPField(isPersistant = true)] public int whx;
        [KSPField(isPersistant = true)] public int why;
        [KSPField(isPersistant = true)] public int tpx;
        [KSPField(isPersistant = true)] public int tpy;
        [KSPField(isPersistant = true)] public int cpx;
        [KSPField(isPersistant = true)] public int cpy;

        [KSPField(isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ResourceManager_UpdateCounter")]//Update Counter
        public long updateCounter;

        protected readonly Dictionary<Guid, double> connectedReceivers = new Dictionary<Guid, double>();
        protected readonly Dictionary<Guid, double> connectedReceiversFraction = new Dictionary<Guid, double>();

        protected List<Part> similarParts;
        protected string[] resourcesToSupply;

        public Guid Id { get; private set;}

        protected int partNrInList;

        private readonly Dictionary<string, double> _resourceSupplied = new Dictionary<string, double>();

        protected void DisableField(string fieldName)
        {
            var field = Fields[fieldName];

            if (field == null) return;
            field.guiActive = false;
            field.guiActiveEditor = false;
        }

        protected void EnableField(string fieldName)
        {
            var field = Fields[fieldName];

            if (field == null) return;
            field.guiActive = true;
            field.guiActiveEditor = true;
        }

        public virtual void AttachThermalReciever(Guid key, double radius)
        {
            if (!connectedReceivers.ContainsKey(key))
                connectedReceivers.Add(key, radius);
        }

        public virtual void DetachThermalReciever(Guid key)
        {
            if (connectedReceivers.ContainsKey(key))
                connectedReceivers.Remove(key);
        }

        public virtual double GetFractionThermalReciever(Guid key)
        {
            if (connectedReceiversFraction.TryGetValue(key, out var result))
                return result;
            else
                return 0;
        }

        public void receiveFNResource(double power, string resourceName)
        {
            if (double.IsNaN(power) || string.IsNullOrEmpty(resourceName)) {
                Debug.Log("[KSPI]: receiveFNResource illegal values.");
                return;
            }

            _resourceSupplied[resourceName] = power;
        }

        public double consumeFNResource(double powerFixed, string resourceName, double fixedDeltaTime = 0)
        {
            if (powerFixed.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.Log("[KSPI]: consumeFNResource illegal values.");
                return 0;
            }

            powerFixed = Math.Max(powerFixed, 0);

            var manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            if (!_resourceSupplied.TryGetValue(resourceName, out var availablePower))
                _resourceSupplied[resourceName] = 0;

            fixedDeltaTime = fixedDeltaTime > 0
                ? Math.Min(PluginSettings.Config.MaxResourceProcessingTimewarp, fixedDeltaTime)
                : Math.Min(PluginSettings.Config.MaxResourceProcessingTimewarp, (double)(decimal)TimeWarp.fixedDeltaTime);

            double powerTakenFixed = Math.Max(Math.Min(powerFixed, availablePower * fixedDeltaTime), 0);

            _resourceSupplied[resourceName] -= powerTakenFixed / fixedDeltaTime;
            manager.PowerDrawFixed(this, powerFixed, powerTakenFixed);

            return powerTakenFixed;
        }

        public double ConsumeFnResourcePerSecond(double powerRequestedPerSecond, string resourceName, ResourceManager manager = null)
        {
            if (powerRequestedPerSecond.IsInfinityOrNaN())
            {
                Debug.Log("[KSPI]: consumeFNResourcePerSecond was called with illegal value");
                return 0;
            }

            if (manager == null)
                manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            if (!_resourceSupplied.TryGetValue(resourceName, out var availablePower))
                _resourceSupplied[resourceName] = 0;

            powerRequestedPerSecond = Math.Max(powerRequestedPerSecond, 0);

            double powerTakenPerSecond = Math.Max(Math.Min(powerRequestedPerSecond, availablePower), 0);
            _resourceSupplied[resourceName] -= powerTakenPerSecond;

            manager.PowerDrawPerSecond(this, powerRequestedPerSecond, powerTakenPerSecond);

            return powerTakenPerSecond;
        }

        public double ConsumeFnResourcePerSecond(double powerRequestedPerSecond, double maximumPowerRequestedPerSecond, string resourceName, ResourceManager manager = null)
        {
            if (powerRequestedPerSecond.IsInfinityOrNaN() || maximumPowerRequestedPerSecond.IsInfinityOrNaN())
            {
                Debug.Log("[KSPI]: consumeFNResourcePerSecond was called with illegal values");
                return 0;
            }

            if (manager == null)
                manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            if (!_resourceSupplied.TryGetValue(resourceName, out var availablePower))
                _resourceSupplied[resourceName] = 0;

            var powerTakenPerSecond = Math.Max(Math.Min(powerRequestedPerSecond, availablePower), 0);
            _resourceSupplied[resourceName] -= powerTakenPerSecond;

            manager.PowerDrawPerSecond(this, powerRequestedPerSecond, Math.Max(maximumPowerRequestedPerSecond, 0), powerTakenPerSecond);

            return powerTakenPerSecond;
        }

        public double ConsumeFnResourcePerSecondBuffered(double requestedPowerPerSecond, string resourceName, double limitBarRatio = 0.1, ResourceManager manager = null)
        {
            double timeWarpFixedDeltaTime = Math.Min(PluginSettings.Config.MaxResourceProcessingTimewarp, (double)(decimal)TimeWarp.fixedDeltaTime);

            if (requestedPowerPerSecond.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.Log("[KSPI]: consumeFNResourcePerSecondBuffered was called with illegal value");
                return 0;
            }

            requestedPowerPerSecond = Math.Max(requestedPowerPerSecond, 0);

            if (manager == null)
                manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            if (!_resourceSupplied.TryGetValue(resourceName, out var availablePower))
                _resourceSupplied[resourceName] = 0;

            var powerTakenPerSecond = Math.Max(Math.Min(requestedPowerPerSecond, availablePower), 0);
            _resourceSupplied[resourceName] -= powerTakenPerSecond;

            // supplement with buffer power if needed and available
            var powerShortage = requestedPowerPerSecond - availablePower;
            if (powerShortage > 0)
            {
                var currentCapacity = manager.GetTotalResourceCapacity();
                var currentAmount = currentCapacity * manager.ResourceFillFraction;
                var fixedPowerShortage = powerShortage * timeWarpFixedDeltaTime;

                if (currentAmount - fixedPowerShortage > currentCapacity * limitBarRatio)
                    powerTakenPerSecond += (part.RequestResource(resourceName, fixedPowerShortage) / timeWarpFixedDeltaTime);
            }

            manager.PowerDrawPerSecond(this, requestedPowerPerSecond, powerTakenPerSecond);

            return powerTakenPerSecond;
        }

        protected double ConsumeMegawatts(double requestedPower, bool allowCapacitor = true, bool allowKilowattHour = false, bool allowEc = false, double fixedDeltaTime = 0)
        {
            if (requestedPower.IsInfinityOrNaN())
                return 0;

            const double kilowattRatio = GameConstants.ecPerMJ / GameConstants.SECONDS_IN_HOUR;
            double dt = fixedDeltaTime > 0
                ? Math.Min(PluginSettings.Config.MaxResourceProcessingTimewarp, fixedDeltaTime)
                : Math.Min(PluginSettings.Config.MaxResourceProcessingTimewarp, TimeWarp.fixedDeltaTime);

            // First try to consume MJ from ResourceManager
            double add, result = CheatOptions.InfiniteElectricity ? requestedPower : ConsumeFnResourcePerSecond(requestedPower, ResourceSettings.Config.ElectricPowerInMegawatt);
            requestedPower -= result;

            // Use MJ from storage such as super capacitors
            if (requestedPower > 0.0 && allowCapacitor)
            {
                add = part.RequestResource(ResourceSettings.Config.ElectricPowerInMegawatt, requestedPower * dt) / dt;
                result += add;
                requestedPower -= add;
            }

            // Use KWH resource from batteries
            if (requestedPower > 0.0 && allowKilowattHour)
            {
                add = part.RequestResource("KilowattHour", requestedPower * kilowattRatio * dt) / (kilowattRatio * dt);
                result += add;
                requestedPower -= add;
            }

            // If still no power, use any electric charge available
            if (requestedPower > 0.0 && allowEc)
            {
                add = part.RequestResource(ResourceSettings.Config.ElectricPowerInKilowatt, requestedPower * GameConstants.ecPerMJ * dt) / (GameConstants.ecPerMJ * dt);
                result += add;
            }

            return result;
        }

        public double SupplyFNResourceFixed(double supply, string resourceName)
        {
            if (supply.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: SupplyFNResourceFixed was called with illegal value");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.PowerSupplyFixed(this, Math.Max(supply, 0));
        }

        public double GetRequiredElectricCharge()
        {
            var manager = (MegajoulesResourceManager)GetManagerForVessel(ResourceSettings.Config.ElectricPowerInMegawatt);
            if (manager == null)
                return 0;

            return manager.MjConverted;
        }

        public double SupplyFnResourcePerSecond(double supply, string resourceName)
        {
            if (supply.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: supplyFNResourcePerSecond  was called with illegal value");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.PowerSupplyPerSecond(this, Math.Max(supply, 0));
        }

        public double SupplyFnResourcePerSecondWithMaxAndEfficiency(double supply, double maxSupply, double efficiencyRatio, string resourceName)
        {
            if (supply.IsInfinityOrNaN() || maxSupply.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: supplyFNResourcePerSecondWithMaxAndEfficiency was called with illegal value");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.PowerSupplyPerSecondWithMaxAndEfficiency(this, Math.Max(supply, 0), Math.Max(maxSupply, 0), efficiencyRatio);
        }

        public double SupplyFnResourcePerSecondWithMax(double supply, double maxSupply, string resourceName)
        {
            if (supply.IsInfinityOrNaN() || maxSupply.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: supplyFNResourcePerSecondWithMax was called with illegal value");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.PowerSupplyPerSecondWithMax(this, Math.Max(supply, 0), Math.Max(maxSupply, 0));
        }

        public double SupplyManagedFnResourcePerSecond(double supply, string resourceName)
        {
            if (supply.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: supplyManagedFNResourcePerSecond  was called with illegal values.");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.ManagedPowerSupplyPerSecond(this, Math.Max(supply, 0));
        }

        public double GetNeededPowerSupplyPerSecondWithMinimumRatio(double supply, double ratioMin, string resourceName, ResourceManager manager = null)
        {
            if (supply.IsInfinityOrNaN() || ratioMin.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: getNeededPowerSupplyPerSecondWithMinimumRatio was called with illegal values.");
                return 0;
            }

            if (manager == null)
                manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.GetNeededPowerSupplyPerSecondWithMinimumRatio(Math.Max(supply, 0), Math.Max(ratioMin, 0));
        }

        public double SupplyManagedFnResourcePerSecondWithMinimumRatio(double supply, double ratioMin, string resourceName, ResourceManager manager = null)
        {
            if (supply.IsInfinityOrNaN() || ratioMin.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: supplyManagedFNResourcePerSecondWithMinimumRatio illegal values.");
                return 0;
            }

            if (manager == null)
                manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.ManagedPowerSupplyPerSecondWithMinimumRatio(this, Math.Max(supply, 0), Math.Max(ratioMin, 0));
        }

        public double ManagedProvidedPowerSupplyPerSecondMinimumRatio(double requestedPower, double maximumPower, double ratioMin, string resourceName, ResourceManager manager = null)
        {
            if (requestedPower.IsInfinityOrNaN()|| maximumPower.IsInfinityOrNaN() || ratioMin.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: managedProvidedPowerSupplyPerSecondMinimumRatio illegal values.");
                return 0;
            }

            if (manager == null)
                manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            var result = manager.ManagedRequestedPowerSupplyPerSecondMinimumRatio(this, requestedPower, Math.Max(maximumPower, 0), Math.Max(ratioMin, 0));

            return result.CurrentSupply;
        }

        public PowerGenerated ManagedPowerSupplyPerSecondMinimumRatio(double requestedPower, double maximumPower, double ratioMin, string resourceName, ResourceManager manager = null)
        {
            if (requestedPower.IsInfinityOrNaN() || maximumPower.IsInfinityOrNaN() || ratioMin.IsInfinityOrNaN() || string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: managedPowerSupplyPerSecondMinimumRatio illegal values.");
                return null;
            }

            if (manager == null)
                manager = GetManagerForVessel(resourceName);

            return manager?.ManagedRequestedPowerSupplyPerSecondMinimumRatio(this, requestedPower, Math.Max(maximumPower, 0), Math.Max(ratioMin, 0));
        }

        public double GetTotalPowerSupplied(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: getTotalPowerSupplied resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.TotalPowerSupplied;
        }

        public double GetStableResourceSupply(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: getCurrentResourceDemand resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.StableResourceSupply;
        }

        public double GetAvailableStableSupply(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return Math.Max(manager.StableResourceSupply - manager.ResourceDemandHighPriority, 0);
        }

        public double GetAvailablePrioritizedStableSupply(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: getAvailablePrioritizedStableSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return Math.Max(manager.StableResourceSupply - manager.GetStablePriorityResourceSupply(getPowerPriority()), 0);
        }

        public double GetAvailablePrioritizedCurrentSupply(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: getAvailablePrioritizedCurrentSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return Math.Max(manager.ResourceSupply - manager.GetCurrentPriorityResourceSupply(getPowerPriority()), 0);
        }

        public double GetCurrentResourceSupply(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName)) {
                Debug.LogError("[KSPI]: getResourceSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return manager.ResourceSupply;
        }

        public double GetCurrentSurplus(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName)) {
                Debug.LogError("[KSPI]: GetCurrentSurplus resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return manager.CurrentSurplus;
        }

        public double GetDemandStableSupply(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName)) {
                Debug.LogError("[KSPI]: getDemandStableSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.DemandStableSupply;
        }

        public double GetCurrentUnfilledResourceDemand(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: GetRequiredResourceDemand resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.CurrentUnfilledResourceDemand;
        }

        public double GetResourceBarRatio(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: getResourceBarRatio resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.ResourceFillFraction;
        }

        public double GetResourceBarFraction(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: getResourceBarFraction resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.ResourceFillFraction;
        }

        public double GetSpareResourceCapacity(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: getSpareResourceCapacity resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.GetSpareResourceCapacity();
        }

        public double GetResourceAvailability(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: getResourceAvailability resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.GetResourceAvailability();
        }

        public double GetTotalResourceCapacity(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: getTotalResourceCapacity resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = GetManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.GetTotalResourceCapacity();
        }

        public override void OnStart(StartState state)
        {
            Id = Guid.NewGuid();

            if (state == StartState.Editor || resourcesToSupply == null) return;

            part.OnJustAboutToBeDestroyed -= OnJustAboutToBeDestroyed;
            part.OnJustAboutToBeDestroyed += OnJustAboutToBeDestroyed;

            foreach (string resourceName in resourcesToSupply)
            {
                ResourceManager manager = getOvermanagerForResource(resourceName).GetManagerForVessel(vessel);

                if (manager != null) continue;

                similarParts = null;
                manager = CreateResourceManagerForResource(resourceName);

                Debug.Log("[KSPI]: ResourceSuppliableModule.OnStart created Resource Manager for Vessel " + vessel.GetName() + " for " + resourceName + " with manager Id " + manager.Id + " and overmanager id " + manager.OverManagerId);
            }

            var priorityManager = getSupplyPriorityManager(vessel);
            priorityManager?.Register(this);
        }

        private void OnJustAboutToBeDestroyed()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            Debug.LogWarning("[KSPI]: detecting supplyable part " + part.partInfo.title + " is being destroyed");

            var priorityManager = getSupplyPriorityManager(vessel);
            priorityManager?.Unregister(this);
        }

        public override void OnFixedUpdate()
        {
            double timeWarpFixedDeltaTime = Math.Min(PluginSettings.Config.MaxResourceProcessingTimewarp, TimeWarp.fixedDeltaTime);

            updateCounter++;

            if (resourcesToSupply == null) return;

            foreach (string resourceName in resourcesToSupply)
            {
                var overmanager = getOvermanagerForResource(resourceName);

                ResourceManager resourceManager = null;;

                if (overmanager != null)
                    resourceManager = overmanager.GetManagerForVessel(vessel);

                if (resourceManager == null)
                {
                    similarParts = null;
                    resourceManager = CreateResourceManagerForResource(resourceName);

                    Debug.Log("[KSPI]: ResourceSuppliableModule.OnFixedUpdate created ResourceManager for Vessel " + vessel.GetName() + " for " + resourceName + " with ResourceManagerId " + resourceManager.Id + " and OvermanagerId" + resourceManager.Id);
                }

                if (resourceManager.PartModule == null || resourceManager.PartModule.vessel != vessel || resourceManager.Counter < updateCounter)
                    resourceManager.UpdatePartModule(this);

                if (resourceManager.PartModule == this)
                    resourceManager.Update(updateCounter);
            }

            var priorityManager = getSupplyPriorityManager(this.vessel);
            if (priorityManager != null)
            {
                priorityManager.Register(this);

                if (priorityManager.ProcessingPart == null || priorityManager.ProcessingPart.vessel != this.vessel || priorityManager.Counter < updateCounter)
                    priorityManager.UpdatePartModule(this);

                if (priorityManager.ProcessingPart == this)
                    priorityManager.UpdateResourceSuppliables(updateCounter, timeWarpFixedDeltaTime);
            }
        }

        public void RemoveItselfAsManager()
        {
            foreach (string resourceName in resourcesToSupply)
            {
                var overmanager = getOvermanagerForResource(resourceName);

                ResourceManager resourceManager = overmanager?.GetManagerForVessel(vessel);

                if (resourceManager != null && resourceManager.PartModule == this)
                    resourceManager.UpdatePartModule(null);
            }
        }

        public virtual string getResourceManagerDisplayName()
        {
            string displayName = part.partInfo.title;

            if (similarParts == null)
            {
                similarParts = vessel.parts.Where(m => m.partInfo.title == part.partInfo.title).ToList();
                partNrInList = 1 + similarParts.IndexOf(part);
            }

            if (similarParts.Count > 1)
                displayName += " " + partNrInList;

            return displayName;
        }

        // default priority
        public virtual int getPowerPriority()
        {
            return 2;
        }

        public virtual int GetSupplyPriority()
        {
            return getPowerPriority();
        }

        private ResourceManager CreateResourceManagerForResource(string resourceName)
        {
            return getOvermanagerForResource(resourceName).CreateManagerForVessel(this);
        }

        private ResourceOvermanager getOvermanagerForResource(string resourceName)
        {
            return ResourceOvermanager.GetResourceOvermanagerForResource(resourceName);
        }

        protected ResourceManager GetManagerForVessel(string resourceName)
        {
            return GetManagerForVessel(resourceName, vessel);
        }

        private ResourceManager GetManagerForVessel(string resourceName, Vessel vessel)
        {
            var overmanager = getOvermanagerForResource(resourceName);
            if (overmanager == null)
            {
                Debug.LogError("[KSPI]: ResourceSuppliableModule failed to find " + resourceName + " Overmanager for " + vessel.name);
                return null;
            }
            return overmanager.GetManagerForVessel(vessel);
        }

        private SupplyPriorityManager getSupplyPriorityManager(Vessel vessel)
        {
            return SupplyPriorityManager.GetSupplyPriorityManagerForVessel(vessel);
        }

        public virtual void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {

        }

        public virtual void OnPostResourceSuppliable(double fixedDeltaTime)
        {

        }
    }
}

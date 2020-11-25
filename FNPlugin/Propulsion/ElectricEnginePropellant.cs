using System;
using System.Collections.Generic;
using System.Linq;

namespace FNPlugin.Propulsion
{
    public enum ElectricEngineType
    {
        PLASMA = 1,
        ARCJET = 2,
        VASIMR = 4,
        VACUUMTHRUSTER = 8,
        RCS = 16,
        ION = 32
    }

    public class ElectricEnginePropellant
    {
        protected bool isInfinite;
        protected int prop_type;
        protected double efficiency;
        protected double ispMultiplier;
        protected double thrustMultiplier;
        protected double decomposedIspMult;
        protected double thrustMultiplierCold;
        protected Propellant propellant;
        protected string propellantname;
        protected string propellantguiname;
        protected string effectname;
        protected double wasteheatMultiplier;
        protected string techRquirement;
        protected PartResourceDefinition resourceDefinition;

        public PartResourceDefinition ResourceDefinition { get { return resourceDefinition; } }

        public int SupportedEngines { get { return prop_type;} }

        public double Efficiency { get { return efficiency; } }

        public double IspMultiplier { get { return ispMultiplier; } }

        public double DecomposedIspMult { get { return decomposedIspMult; } }

        public double ThrustMultiplier { get { return thrustMultiplier; } }

        public double ThrustMultiplierCold { get { return thrustMultiplierCold; } }

        public Propellant Propellant {  get { return propellant; } }

        public String PropellantName { get { return propellantname; } }

        public String PropellantGUIName { get { return propellantguiname; } }

        public String ParticleFXName { get { return effectname; } }

        public double WasteHeatMultiplier { get { return wasteheatMultiplier; } }

        public string TechRequirement { get { return techRquirement; } }

        public bool IsInfinite { get { return isInfinite; } }

        public ElectricEnginePropellant(ConfigNode node)
        {
            propellantname = node.GetValue("name");

            propellantguiname = node.HasValue("guiName") ? node.GetValue("guiName") : propellantname;
            isInfinite = node.HasValue("isInfinite") ? Convert.ToBoolean(node.GetValue("isInfinite")) : false;
            ispMultiplier = node.HasValue("ispMultiplier") ? Convert.ToSingle(node.GetValue("ispMultiplier")) : 1;
            decomposedIspMult = node.HasValue("decomposedIspMult") ? Convert.ToDouble(node.GetValue("decomposedIspMult")) : ispMultiplier;
            thrustMultiplier = node.HasValue("thrustMultiplier") ? Convert.ToDouble(node.GetValue("thrustMultiplier")) : 1;
            thrustMultiplierCold = node.HasValue("thrustMultiplierCold") ? Convert.ToDouble(node.GetValue("thrustMultiplierCold")) : thrustMultiplier;
            wasteheatMultiplier = node.HasValue("wasteheatMultiplier") ? Convert.ToDouble(node.GetValue("wasteheatMultiplier")) : 1;
            efficiency = node.HasValue("efficiency") ? Convert.ToDouble(node.GetValue("efficiency")) : 1;
            prop_type = node.HasValue("type") ? Convert.ToInt32(node.GetValue("type")) : 1;
            effectname = node.HasValue("effectName")  ? node.GetValue("effectName") : "none";
            techRquirement = node.HasValue("techRequirement") ? node.GetValue("techRequirement") : String.Empty;

            ConfigNode propellantnode = node.GetNode("PROPELLANT");
            propellant = new Propellant();
            propellant.Load(propellantnode);
        }


        public static List<ElectricEnginePropellant> GetPropellantsEngineForType(int type)
        {
            ConfigNode[] propellantlist = GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT");
            List<ElectricEnginePropellant> propellant_list;
            if (propellantlist.Length == 0)
            {
                PluginHelper.showInstallationErrorMessage();
                propellant_list = new List<ElectricEnginePropellant>();
            }
            else
            {
                propellant_list = propellantlist.Select(prop => new ElectricEnginePropellant(prop))
                    .Where(eep => (eep.SupportedEngines & type) == type && PluginHelper.HasTechRequirementOrEmpty(eep.TechRequirement)).ToList();
            }

            // initialize resource Defnitionions
            foreach (var propellant in propellant_list)
            {
                propellant.resourceDefinition = PartResourceLibrary.Instance.GetDefinition(propellant.propellant.name);
            }

            return propellant_list;
        }

    }
}

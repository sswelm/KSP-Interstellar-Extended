using FNPlugin.Redist;
using System;
using System.Linq;
using FNPlugin.Powermanagement;
using FNPlugin.Reactors;

namespace FNPlugin.Extensions
{
    public class PowerSourceSearchResult
    {
        public PowerSourceSearchResult(IFNPowerSource source, float cost)
        {
            Cost = cost;
            Source = source;
        }

        public double Cost { get; private set; }
        public IFNPowerSource Source { get; private set; }

        public PowerSourceSearchResult IncreaseCost(double cost)
        {
            Cost += cost;
            return this;
        }

        public static PowerSourceSearchResult BreadthFirstSearchForThermalSource(Part currentpart, Func<IFNPowerSource, bool> condition, int stackdepth, int parentdepth, int surfacedepth, bool skipSelfContained = false)
        {
            // first search without parent search
            for (int currentDepth = 0; currentDepth <= stackdepth; currentDepth++)
            {
                var source = FindThermalSource(null, currentpart, condition, currentDepth, parentdepth, surfacedepth, skipSelfContained);

                if (source != null)
                    return source;
            }

            return null;
        }

        public static PowerSourceSearchResult FindThermalSource(Part previousPart, Part currentpart, Func<IFNPowerSource, bool> condition, int stackdepth, int parentdepth, int surfacedepth, bool skipSelfContained)
        {
            if (stackdepth <= 0)
            {
                var thermalsources = currentpart.FindModulesImplementing<IFNPowerSource>().Where(condition);

                var source = skipSelfContained
                    ? thermalsources.FirstOrDefault(s => !s.IsSelfContained)
                    : thermalsources.FirstOrDefault();

                if (source != null)
                    return new PowerSourceSearchResult(source, 0);
                else
                    return null;
            }

            var thermalcostModifier = currentpart.FindModuleImplementing<ThermalPowerTransport>();

            double stackDepthCost = thermalcostModifier != null 
                ? thermalcostModifier.thermalCost 
                : 1;

            // first look at docked parts
            foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null && atn.attachedPart != previousPart && atn.nodeType == AttachNode.NodeType.Dock))
            {
                var source = FindThermalSource(currentpart, attachNodes.attachedPart, condition, (stackdepth - 1), parentdepth, surfacedepth, skipSelfContained);

                if (source != null)
                    return source.IncreaseCost(stackDepthCost);
            }

            // then look at stack attached parts
            foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null && atn.attachedPart != previousPart && atn.nodeType == AttachNode.NodeType.Stack))
            {
                var source = FindThermalSource(currentpart, attachNodes.attachedPart, condition, (stackdepth - 1), parentdepth, surfacedepth, skipSelfContained);

                if (source != null)
                    return source.IncreaseCost(stackDepthCost);
            }

            // then look at parent parts
            if (parentdepth > 0 && currentpart.parent != null && currentpart.parent != previousPart)
            {
                var source = FindThermalSource(currentpart, currentpart.parent, condition, (stackdepth - 1), (parentdepth - 1), surfacedepth, skipSelfContained);

                if (source != null)
                    return source.IncreaseCost(stackDepthCost);
            }

            // then look at surface attached parts
            if (surfacedepth > 0)
            {
                foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null && atn.attachedPart != previousPart && atn.nodeType == AttachNode.NodeType.Surface))
                {
                    var source = FindThermalSource(currentpart, attachNodes.attachedPart, condition, (stackdepth - 1), parentdepth, (surfacedepth - 1), skipSelfContained);

                    if (source != null)
                        return source.IncreaseCost(stackDepthCost);
                }
            }

            return null;
        }
    }
}

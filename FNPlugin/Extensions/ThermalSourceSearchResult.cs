using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Extensions
{
    public class PowerSourceSearchResult
    {
        public PowerSourceSearchResult(IPowerSource source, float cost)
        {
            Cost = cost;
            Source = source;
        }

        public float Cost { get; private set; }
        public IPowerSource Source { get; private set; }

        public PowerSourceSearchResult IncreaseCost(float cost)
        {
            Cost += cost;
            return this;
        }

        public static PowerSourceSearchResult BreadthFirstSearchForThermalSource(Part currentpart, Func<IPowerSource, bool> condition, int stackdepth, int parentdepth, int surfacedepth, bool skipSelfContained = false)
        {
            // first search withouth parent search
            for (int currentDepth = 0; currentDepth <= stackdepth; currentDepth++)
            {
                var source = FindThermalSource(currentpart, condition, currentDepth, parentdepth, surfacedepth, skipSelfContained);

                if (source != null)
                    return source;
            }

            return null;
        }

        public static PowerSourceSearchResult FindThermalSource(Part currentpart, Func<IPowerSource, bool> condition, int stackdepth, int parentdepth, int surfacedepth, bool skipSelfContained)
        {
            if (stackdepth <= 0)
            {
                var thermalsources = currentpart.FindModulesImplementing<IPowerSource>().Where(condition);

                var source = skipSelfContained
                    ? thermalsources.FirstOrDefault(s => !s.IsSelfContained)
                    : thermalsources.FirstOrDefault();

                if (source != null)
                    return new PowerSourceSearchResult(source, 0);
                else
                    return null;
            }

            var thermalcostModifier = currentpart.FindModuleImplementing<ThermalPowerTransport>();

            float stackDepthCost = thermalcostModifier != null 
                ? thermalcostModifier.thermalCost 
                : 1;

            // first look at stack attached parts
            foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null && (atn.nodeType == AttachNode.NodeType.Stack || atn.nodeType == AttachNode.NodeType.Dock)))
            {
                var source = FindThermalSource(attachNodes.attachedPart, condition, (stackdepth - 1), parentdepth, surfacedepth, skipSelfContained);

                if (source != null)
                    return source.IncreaseCost(stackDepthCost);
            }

            // then optionaly look at parent parts
            if (parentdepth > 0 && currentpart.parent != null)
            {
                var source = FindThermalSource(currentpart.parent, condition, (stackdepth - 1), (parentdepth - 1), surfacedepth, skipSelfContained);

                if (source != null)
                    return source.IncreaseCost(stackDepthCost);
            }

            // then optionaly look at surface attached parts
            if (surfacedepth > 0)
            {
                foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null && atn.nodeType == AttachNode.NodeType.Surface))
                {
                    var source = FindThermalSource(attachNodes.attachedPart, condition, (stackdepth - 1), parentdepth, (surfacedepth - 1), skipSelfContained);

                    if (source != null)
                        return source.IncreaseCost(stackDepthCost);
                }
            }

            return null;
        }
    }
}

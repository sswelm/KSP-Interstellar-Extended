using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Extensions
{
    public class ThermalSourceSearchResult
    {
        public ThermalSourceSearchResult(IThermalSource source, float cost)
        {
            Cost = cost;
            Source = source;
        }

        public float Cost { get; private set; }
        public IThermalSource Source { get; private set; }

        public ThermalSourceSearchResult IncreaseCost(float cost)
        {
            Cost += cost;
            return this;
        }

        public static ThermalSourceSearchResult BreadthFirstSearchForThermalSource(Part currentpart, Func<IThermalSource, bool> condition, int stackdepth, int parentdepth, int childdepth, int attachdepth, bool skipSelfContained = false)
        {
            // first search withouth parent search
            for (int currentDepth = 0; currentDepth <= stackdepth; currentDepth++)
            {
                var source = FindThermalSource(currentpart, condition, currentDepth, parentdepth, childdepth, attachdepth, skipSelfContained);

                if (source != null)
                    return source;
            }

            return null;
        }

        public static ThermalSourceSearchResult FindThermalSource(Part currentpart, Func<IThermalSource, bool> condition, int stackdepth, int parentdepth, int childdepth, int attachdepth, bool skipSelfContained )
        {
            if (stackdepth == 0)
            {
                var thermalsources = currentpart.FindModulesImplementing<IThermalSource>().Where(condition);

                var source = skipSelfContained 
                    ? thermalsources.FirstOrDefault(s => !s.IsSelfContained) 
                    : thermalsources.FirstOrDefault();

                if (source != null)
                    return new ThermalSourceSearchResult(source, 0);
                else
                    return null;
            }

            var thermalcostModifier = currentpart.FindModuleImplementing<ThermalPowerTransport>();

            float stackDepthCost = thermalcostModifier != null ? thermalcostModifier.thermalCost : 1;

            // look at parent
            if (parentdepth > 0 && currentpart.parent != null)
            {
                var source = FindThermalSource(currentpart.parent, condition, (stackdepth - 1), (parentdepth - 1), childdepth, attachdepth, skipSelfContained);

                if (source != null)
                    return source.IncreaseCost(1);
            }

            // look at childs
            if (childdepth > 0 && currentpart.children != null)
            {
                foreach (var child in currentpart.children)
                {
                    var source = FindThermalSource(child, condition, (stackdepth - 1), parentdepth, (childdepth - 1), attachdepth, skipSelfContained);

                    if (source != null)
                        return source.IncreaseCost(1);
                }
            }

            // look at attachments
            if (attachdepth > 0)
            {
                foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null))
                {
                    var source = FindThermalSource(attachNodes.attachedPart, condition, (stackdepth - 1), parentdepth, childdepth, (attachdepth - 1), skipSelfContained);

                    if (source != null)
                        return source.IncreaseCost(stackDepthCost);
                }
            }

            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Drift.Contract.MappingDriftService.Builder;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;

namespace ThreatFramework.Drift.Impl.MappingDriftService.Builder
{
    public sealed class ComponentPropertyGraphBuilder : IComponentPropertyGraphBuilder
    {
        public ComponentPropertyGraph Build(
            IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping> srRows,
            IEnumerable<ComponentPropertyOptionThreatMapping> threatRows,
            IEnumerable<ComponentPropertyOptionMapping> optionRows,
            IEnumerable<ComponentPropertyMapping> propertyRows)
        {
            var root = new Dictionary<Guid, Dictionary<Guid, Dictionary<Guid, Dictionary<Guid, List<Guid>>>>>();

            static TOut GetOrAdd<TIn, TOut>(IDictionary<TIn, TOut> d, TIn key, Func<TOut> factory)
                where TIn : notnull
            {
                if (!d.TryGetValue(key, out var v)) { v = factory(); d[key] = v; }
                return v!;
            }

            // 1) SR rows (most specific)
            foreach (var r in srRows)
            {
                var byProperty = GetOrAdd(root, r.ComponentGuid, () => new());
                var byOption = GetOrAdd(byProperty, r.PropertyGuid, () => new());
                var byThreat = GetOrAdd(byOption, r.PropertyOptionGuid, () => new());
                var srList = GetOrAdd(byThreat, r.ThreatGuid, () => new List<Guid>());

                if (!srList.Contains(r.SecurityRequirementGuid))
                    srList.Add(r.SecurityRequirementGuid);
            }

            // 2) Threat rows (ensure presence, possibly empty SR list)
            foreach (var r in threatRows)
            {
                var byProperty = GetOrAdd(root, r.ComponentGuid, () => new());
                var byOption = GetOrAdd(byProperty, r.PropertyGuid, () => new());
                var byThreat = GetOrAdd(byOption, r.PropertyOptionGuid, () => new());
                GetOrAdd(byThreat, r.ThreatGuid, () => new List<Guid>());
            }

            // 3) Option rows
            foreach (var r in optionRows)
            {
                var byProperty = GetOrAdd(root, r.ComponentGuid, () => new());
                GetOrAdd(byProperty, r.PropertyGuid, () => new());
                var byOption = root[r.ComponentGuid][r.PropertyGuid];
                GetOrAdd(byOption, r.PropertyOptionGuid, () => new());
            }

            // 4) Property rows
            foreach (var r in propertyRows)
            {
                var byProperty = GetOrAdd(root, r.ComponentGuid, () => new());
                GetOrAdd(byProperty, r.PropertyGuid, () => new());
            }

            // Optional: sort SRs for deterministic output/tests
            foreach (var byProperty in root.Values)
                foreach (var byOption in byProperty.Values)
                    foreach (var byThreat in byOption.Values)
                        foreach (var srList in byThreat.Values)
                            srList.Sort();

            return new ComponentPropertyGraph(root);
        }
    }
}

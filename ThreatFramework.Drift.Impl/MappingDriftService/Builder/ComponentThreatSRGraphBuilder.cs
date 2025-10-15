using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.ComponentMapping;
using ThreatFramework.Drift.Contract.MappingDriftService.Builder;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;

namespace ThreatFramework.Drift.Impl.MappingDriftService.Builder
{
    public sealed class ComponentThreatSRGraphBuilder : IComponentThreatSRGraphBuilder
    {
        public ComponentThreatSRGraph Build(
            IEnumerable<ComponentThreatSecurityRequirementMapping> srRows,
            IEnumerable<ComponentThreatMapping> threatRows)
        {
            var root = new Dictionary<Guid, Dictionary<Guid, List<Guid>>>();

            static TOut GetOrAdd<TIn, TOut>(IDictionary<TIn, TOut> d, TIn k, Func<TOut> f)
                where TIn : notnull
            {
                if (!d.TryGetValue(k, out var v)) { v = f(); d[k] = v; }
                return v!;
            }

            // 1) SR rows (most specific): ensure (Component, Threat) exists and add SR
            foreach (var r in srRows)
            {
                var byThreat = GetOrAdd(root, r.ComponentGuid, () => new());
                var srList = GetOrAdd(byThreat, r.ThreatGuid, () => new List<Guid>());

                if (!srList.Contains(r.SecurityRequirementGuid))
                    srList.Add(r.SecurityRequirementGuid);
            }

            // 2) Threat rows (shape only; may remain with empty SR list)
            foreach (var r in threatRows)
            {
                var byThreat = GetOrAdd(root, r.ComponentGuid, () => new());
                GetOrAdd(byThreat, r.ThreatGuid, () => new List<Guid>());
                // NOTE: We are not filtering by IsHidden/IsOverridden/UsedForMitigation here.
            }

            // Sort SR lists for deterministic results/tests
            foreach (var byThreat in root.Values)
                foreach (var list in byThreat.Values)
                    list.Sort();

            return new ComponentThreatSRGraph(root);
        }
    }
}

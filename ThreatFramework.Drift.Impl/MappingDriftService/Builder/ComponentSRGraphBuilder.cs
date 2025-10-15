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
    public sealed class ComponentSRGraphBuilder : IComponentSRGraphBuilder
    {
        public ComponentSRGraph Build(IEnumerable<ComponentSecurityRequirementMapping> rows)
        {
            var root = new Dictionary<Guid, List<Guid>>();

            static TOut GetOrAdd<TIn, TOut>(IDictionary<TIn, TOut> d, TIn k, Func<TOut> f)
                where TIn : notnull
            {
                if (!d.TryGetValue(k, out var v)) { v = f(); d[k] = v; }
                return v!;
            }

            // Aggregate with a temporary HashSet per component for deduping
            var temp = new Dictionary<Guid, HashSet<Guid>>();

            foreach (var r in rows)
            {
                var set = GetOrAdd(temp, r.ComponentGuid, () => new HashSet<Guid>());
                set.Add(r.SecurityRequirementGuid);
            }

            // Materialize to sorted lists
            foreach (var (compId, set) in temp)
            {
                var list = set.ToList();
                list.Sort();
                root[compId] = list;
            }

            return new ComponentSRGraph(root);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.MappingDriftService;
using ThreatFramework.Drift.Contract.MappingDriftService.Dto;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;
using ThreatFramework.Drift.Impl.MappingDriftService.Helper;

namespace ThreatFramework.Drift.Impl.MappingDriftService
{
    public sealed class ComponentThreatSRDriftService : IComponentThreatSRDriftService
    {
        private readonly ISetDiffer<ThreatSREdge> _differ;

        public ComponentThreatSRDriftService() : this(new HashSetDiffer<ThreatSREdge>()) { }
        public ComponentThreatSRDriftService(ISetDiffer<ThreatSREdge> differ) => _differ = differ;

        public List<ComponentThreatSRDriftDto> ComputeDrift(
            ComponentThreatSRGraph sourceA,
            ComponentThreatSRGraph sourceB)
        {
            if (sourceA is null) throw new ArgumentNullException(nameof(sourceA));
            if (sourceB is null) throw new ArgumentNullException(nameof(sourceB));

            var compIds = new HashSet<Guid>(sourceA.Components);
            compIds.UnionWith(sourceB.Components);

            var results = new List<ComponentThreatSRDriftDto>(compIds.Count);

            foreach (var compId in compIds.OrderBy(x => x))
            {
                var a = sourceA.GetEdgesForComponent(compId);
                var b = sourceB.GetEdgesForComponent(compId);
                var (added, removed) = _differ.Diff(a, b);

                if (added.Count == 0 && removed.Count == 0) continue;

                results.Add(new ComponentThreatSRDriftDto
                {
                    ComponentGuid = compId,
                    Added = added
                        .OrderBy(e => e.ThreatId).ThenBy(e => e.SRId)
                        .Select(e => new ThreatSRMapping(e.ThreatId, e.SRId))
                        .ToList(),
                    Removed = removed
                        .OrderBy(e => e.ThreatId).ThenBy(e => e.SRId)
                        .Select(e => new ThreatSRMapping(e.ThreatId, e.SRId))
                        .ToList()
                });
            }

            return results;
        }
    }
}
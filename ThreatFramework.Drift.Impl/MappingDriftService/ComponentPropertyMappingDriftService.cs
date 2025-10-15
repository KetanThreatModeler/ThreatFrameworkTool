using ThreatFramework.Drift.Contract.MappingDriftService;
using ThreatFramework.Drift.Contract.MappingDriftService.Dto;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;
using ThreatFramework.Drift.Impl.MappingDriftService.Helper;

namespace ThreatFramework.Drift.Impl.MappingDriftService
{
    public class ComponentPropertyMappingDriftService : IComponentPropertyMappingDriftService
    {
        private readonly ISetDiffer<ComponentPropertyMappingEdge> _differ;

        public ComponentPropertyMappingDriftService() : this(new HashSetDiffer<ComponentPropertyMappingEdge>()) { }
        public ComponentPropertyMappingDriftService(ISetDiffer<ComponentPropertyMappingEdge> differ)
        {
            _differ = differ ?? throw new ArgumentNullException(nameof(differ));
        }




        public List<ComponentPropertyThSrDiffDto> ComputeDrift(ComponentPropertyGraph sourceA, ComponentPropertyGraph sourceB)
        {
            if (sourceA is null) throw new ArgumentNullException(nameof(sourceA));
            if (sourceB is null) throw new ArgumentNullException(nameof(sourceB));

            var compIds = new HashSet<Guid>(sourceA.Components);
            compIds.UnionWith(sourceB.Components);

            var results = new List<ComponentPropertyThSrDiffDto>(compIds.Count);

            foreach (var compId in compIds.OrderBy(x => x))
            {
                var a = sourceA.GetEdgesForComponent(compId);
                var b = sourceB.GetEdgesForComponent(compId);

                var (added, removed) = _differ.Diff(a, b);

                if (added.Count == 0 && removed.Count == 0) continue;

                results.Add(new ComponentPropertyThSrDiffDto
                {
                    ComponentGuid = compId,
                    PropertyMappingsAdded = added
                        .OrderBy(e => e.PropertyId)
                        .ThenBy(e => e.PropertyOptionId)
                        .ThenBy(e => e.ThreatId)
                        .ThenBy(e => e.SRId)
                        .Select(e => new PropertyThreatSRMapping(e.PropertyId, e.PropertyOptionId, e.ThreatId, e.SRId))
                        .ToList(),
                    PropertyMappingsRemoved = removed
                        .OrderBy(e => e.PropertyId)
                        .ThenBy(e => e.PropertyOptionId)
                        .ThenBy(e => e.ThreatId)
                        .ThenBy(e => e.SRId)
                        .Select(e => new PropertyThreatSRMapping(e.PropertyId, e.PropertyOptionId, e.ThreatId, e.SRId))
                        .ToList()
                });
            }

            return results;
        }
    }
}




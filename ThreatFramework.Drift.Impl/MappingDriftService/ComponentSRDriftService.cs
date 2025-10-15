using ThreatFramework.Drift.Contract.MappingDriftService;
using ThreatFramework.Drift.Contract.MappingDriftService.Dto;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;
using ThreatFramework.Drift.Impl.MappingDriftService.Helper;

namespace ThreatFramework.Drift.Impl.MappingDriftService
{
    public sealed class ComponentSRDriftService : IComponentSRDriftService
    {
        private readonly ISetDiffer<ComponentSREdge> _differ;

        public ComponentSRDriftService() : this(new HashSetDiffer<ComponentSREdge>()) { }
        public ComponentSRDriftService(ISetDiffer<ComponentSREdge> differ)
        {
            _differ = differ ?? throw new ArgumentNullException(nameof(differ));
        }

        public List<ComponentSRDriftDto> ComputeDrift(ComponentSRGraph sourceA, ComponentSRGraph sourceB)
        {
            if (sourceA is null) throw new ArgumentNullException(nameof(sourceA));
            if (sourceB is null) throw new ArgumentNullException(nameof(sourceB));

            var compIds = new HashSet<Guid>(sourceA.Components);
            compIds.UnionWith(sourceB.Components);

            var results = new List<ComponentSRDriftDto>(compIds.Count);

            foreach (var compId in compIds.OrderBy(x => x))
            {
                var a = sourceA.GetEdgesForComponent(compId);
                var b = sourceB.GetEdgesForComponent(compId);
                var (added, removed) = _differ.Diff(a, b);

                if (added.Count == 0 && removed.Count == 0) continue;

                results.Add(new ComponentSRDriftDto
                {
                    ComponentGuid = compId,
                    Added = added.Select(e => e.SRId).OrderBy(x => x).ToList(),
                    Removed = removed.Select(e => e.SRId).OrderBy(x => x).ToList()
                });
            }

            return results;
        }
    }
}

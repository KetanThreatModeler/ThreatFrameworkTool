using ThreatFramework.Drift.Contract.MappingDriftService;
using ThreatFramework.Drift.Contract.MappingDriftService.Dto;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;

namespace ThreatFramework.Drift.Impl.MappingDriftService
{
    public sealed class ComponentMappingDriftAggregator : IComponentMappingDriftAggregator
    {
        public List<ComponentMappingDriftDto> Compose(
         IEnumerable<ComponentPropertyThSrDiffDto> propertyDrift,
         IEnumerable<ComponentThreatSRDriftDto> componentThreatDrift,
         IEnumerable<ComponentSRDriftDto> componentSrDrift)
        {
            propertyDrift ??= Enumerable.Empty<ComponentPropertyThSrDiffDto>();
            componentThreatDrift ??= Enumerable.Empty<ComponentThreatSRDriftDto>();
            componentSrDrift ??= Enumerable.Empty<ComponentSRDriftDto>();

            var secAddedByComp = new Dictionary<Guid, HashSet<Guid>>();
            var secRemovedByComp = new Dictionary<Guid, HashSet<Guid>>();
            var thAddedByComp = new Dictionary<Guid, HashSet<(Guid ThreatId, Guid? SRId)>>();
            var thRemovedByComp = new Dictionary<Guid, HashSet<(Guid ThreatId, Guid? SRId)>>();
            var propAddedByComp = new Dictionary<Guid, HashSet<(Guid PropertyId, Guid OptionId, Guid ThreatId, Guid SRId)>>();
            var propRemovedByComp = new Dictionary<Guid, HashSet<(Guid PropertyId, Guid OptionId, Guid ThreatId, Guid SRId)>>();

            static HashSet<T> GetSet<TKey, T>(Dictionary<TKey, HashSet<T>> dict, TKey key)
                where TKey : notnull
            {
                if (!dict.TryGetValue(key, out var set))
                {
                    set = new HashSet<T>();
                    dict[key] = set;
                }
                return set;
            }

            // Component → SR drift
            foreach (var dto in componentSrDrift)
            {
                var addSet = GetSet(secAddedByComp, dto.ComponentGuid);
                foreach (var sr in dto.Added) addSet.Add(sr);

                var remSet = GetSet(secRemovedByComp, dto.ComponentGuid);
                foreach (var sr in dto.Removed) remSet.Add(sr);
            }

            // Component → Threat → SR drift (ThreatSRMapping.SRId can be null)
            foreach (var dto in componentThreatDrift)
            {
                var addSet = GetSet(thAddedByComp, dto.ComponentGuid);
                foreach (var m in dto.Added) addSet.Add((m.ThreatId, m.SRId));

                var remSet = GetSet(thRemovedByComp, dto.ComponentGuid);
                foreach (var m in dto.Removed) remSet.Add((m.ThreatId, m.SRId));
            }

            // Component → Property → Option → Threat → SR drift
            foreach (var dto in propertyDrift)
            {
                var addSet = GetSet(propAddedByComp, dto.ComponentGuid);
                foreach (var m in dto.PropertyMappingsAdded)
                    addSet.Add((m.PropertyId, m.PropertyOptionId, m.ThreatId, m.SRId));

                var remSet = GetSet(propRemovedByComp, dto.ComponentGuid);
                foreach (var m in dto.PropertyMappingsRemoved)
                    remSet.Add((m.PropertyId, m.PropertyOptionId, m.ThreatId, m.SRId));
            }

            // All components that appear anywhere
            var componentIds = new HashSet<Guid>();
            componentIds.UnionWith(secAddedByComp.Keys);
            componentIds.UnionWith(secRemovedByComp.Keys);
            componentIds.UnionWith(thAddedByComp.Keys);
            componentIds.UnionWith(thRemovedByComp.Keys);
            componentIds.UnionWith(propAddedByComp.Keys);
            componentIds.UnionWith(propRemovedByComp.Keys);

            static IEnumerable<Guid> OrderGuids(IEnumerable<Guid> guids) =>
                guids.OrderBy(x => x);

            static IEnumerable<ThreatSRMapping> OrderThreatSRs(IEnumerable<(Guid ThreatId, Guid? SRId)> tuples) =>
                tuples
                    .OrderBy(t => t.ThreatId)
                    .ThenBy(t => t.SRId.HasValue ? 0 : 1)    // SR present first, then null
                    .ThenBy(t => t.SRId ?? Guid.Empty)
                    .Select(t => t.SRId.HasValue
                        ? new ThreatSRMapping(t.ThreatId, t.SRId)
                        : new ThreatSRMapping(t.ThreatId));

            static IEnumerable<PropertyThreatSRMapping> OrderPropThreatSRs(IEnumerable<(Guid PropertyId, Guid OptionId, Guid ThreatId, Guid SRId)> tuples) =>
                tuples
                    .OrderBy(t => t.PropertyId)
                    .ThenBy(t => t.OptionId)
                    .ThenBy(t => t.ThreatId)
                    .ThenBy(t => t.SRId)
                    .Select(t => new PropertyThreatSRMapping(t.PropertyId, t.OptionId, t.ThreatId, t.SRId));

            var output = new List<ComponentMappingDriftDto>(componentIds.Count);

            foreach (var compId in componentIds.OrderBy(x => x))
            {
                secAddedByComp.TryGetValue(compId, out var secAdded);
                secRemovedByComp.TryGetValue(compId, out var secRemoved);
                thAddedByComp.TryGetValue(compId, out var thAdded);
                thRemovedByComp.TryGetValue(compId, out var thRemoved);
                propAddedByComp.TryGetValue(compId, out var propAdded);
                propRemovedByComp.TryGetValue(compId, out var propRemoved);

                output.Add(new ComponentMappingDriftDto
                {
                    ComponentGuid = compId,
                    SecurityRequirementsAdded = secAdded is null ? new List<Guid>() : OrderGuids(secAdded).ToList(),
                    SecurityRequirementsRemoved = secRemoved is null ? new List<Guid>() : OrderGuids(secRemoved).ToList(),
                    MappingsAdded = thAdded is null ? new List<ThreatSRMapping>() : OrderThreatSRs(thAdded).ToList(),
                    MappingsRemoved = thRemoved is null ? new List<ThreatSRMapping>() : OrderThreatSRs(thRemoved).ToList(),
                    PropertyThreatSRMappingsAdded = propAdded is null ? new List<PropertyThreatSRMapping>() : OrderPropThreatSRs(propAdded).ToList(),
                    PropertyThreatSRMappingsRemoved = propRemoved is null ? new List<PropertyThreatSRMapping>() : OrderPropThreatSRs(propRemoved).ToList()
                });
            }

            return output;
        }
    }
}
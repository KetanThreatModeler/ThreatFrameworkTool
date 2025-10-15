using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.MappingDriftService.Model
{
    public sealed class ComponentPropertyGraph
    {
        // We keep the internal representation private and never leak this type.
        private readonly Dictionary<Guid, Dictionary<Guid, Dictionary<Guid, Dictionary<Guid, List<Guid>>>>> _data;

        public ComponentPropertyGraph(
            Dictionary<Guid, Dictionary<Guid, Dictionary<Guid, Dictionary<Guid, List<Guid>>>>> data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        /// Enumerate all components present in the graph.
        /// </summary>
        public IEnumerable<Guid> Components => _data.Keys;

        /// <summary>
        /// Get all leaf edges for a given component (ignores threats with zero SRs).
        /// </summary>
        public HashSet<ComponentPropertyMappingEdge> GetEdgesForComponent(Guid componentId)
        {
            var result = new HashSet<ComponentPropertyMappingEdge>();
            if (!_data.TryGetValue(componentId, out var byProperty)) return result;

            foreach (var (propertyId, byOption) in byProperty)
                foreach (var (optionId, byThreat) in byOption)
                    foreach (var (threatId, srList) in byThreat)
                    {
                        if (srList is null || srList.Count == 0) continue;
                        foreach (var srId in srList)
                            result.Add(new ComponentPropertyMappingEdge(componentId, propertyId, optionId, threatId, srId));
                    }

            return result;
        }
    }

    public readonly record struct ComponentPropertyMappingEdge(
      Guid ComponentId,
      Guid PropertyId,
      Guid PropertyOptionId,
      Guid ThreatId,
      Guid SRId);
}

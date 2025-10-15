using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.MappingDriftService.Model
{
    public readonly record struct ThreatSREdge(Guid ComponentId, Guid ThreatId, Guid SRId);

    // ===== Strongly-typed wrapper over: Component -> Threat -> List<SR> =====
    public sealed class ComponentThreatSRGraph
    {
        private readonly Dictionary<Guid, Dictionary<Guid, List<Guid>>> _data;

        public ComponentThreatSRGraph(Dictionary<Guid, Dictionary<Guid, List<Guid>>> data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public IEnumerable<Guid> Components => _data.Keys;

        /// Expose dictionary if you need to persist/inspect it.
        public Dictionary<Guid, Dictionary<Guid, List<Guid>>> AsDictionary() => _data;

        /// Flatten to leaf edges for a component (ignores threats with zero SRs).
        public HashSet<ThreatSREdge> GetEdgesForComponent(Guid componentId)
        {
            var edges = new HashSet<ThreatSREdge>();
            if (!_data.TryGetValue(componentId, out var byThreat)) return edges;

            foreach (var (threatId, srList) in byThreat)
            {
                if (srList is null || srList.Count == 0) continue;
                foreach (var sr in srList)
                    edges.Add(new ThreatSREdge(componentId, threatId, sr));
            }
            return edges;
        }
    }
}

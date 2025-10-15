using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.MappingDriftService.Model
{
    public readonly record struct ComponentSREdge(Guid ComponentId, Guid SRId);

    /// Strongly-typed wrapper over: Component -> List<SR>.
    /// Internally we store: Dictionary<Guid, List<Guid>> (de-duped & sorted).
    public sealed class ComponentSRGraph
    {
        private readonly Dictionary<Guid, List<Guid>> _data;

        public ComponentSRGraph(Dictionary<Guid, List<Guid>> data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// Underlying dictionary (your requested shape).
        public Dictionary<Guid, List<Guid>> AsDictionary() => _data;

        public IEnumerable<Guid> Components => _data.Keys;

        /// Returns leaf edges for a component.
        public HashSet<ComponentSREdge> GetEdgesForComponent(Guid componentId)
        {
            var edges = new HashSet<ComponentSREdge>();
            if (!_data.TryGetValue(componentId, out var srList) || srList.Count == 0)
                return edges;

            foreach (var sr in srList)
                edges.Add(new ComponentSREdge(componentId, sr));

            return edges;
        }
    }
}

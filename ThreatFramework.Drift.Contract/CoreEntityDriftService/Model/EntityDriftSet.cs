using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.CoreEntityDriftService.Model
{
    public class EntityDriftSet<T> where T : class
    {
        public List<T> Added { get; } = new();
        public List<T> Removed { get; } = new();
        public List<EntityPair<T>> Modified { get; } = new();
    }

    public class EntityPair<T> where T : class
    {
        public T Existing { get; set; } = default!;
        public T Updated { get; set; } = default!;
    }
}

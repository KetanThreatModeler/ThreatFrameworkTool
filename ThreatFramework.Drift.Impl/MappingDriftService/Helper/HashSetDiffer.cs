using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Impl.MappingDriftService.Helper
{
    public sealed class HashSetDiffer<T> : ISetDiffer<T>
    {
        public (List<T> Added, List<T> Removed) Diff(HashSet<T> a, HashSet<T> b)
        {
            var added = new List<T>(a.Count);
            var removed = new List<T>(b.Count);

            foreach (var x in a) if (!b.Contains(x)) removed.Add(x);
            foreach (var x in b) if (!a.Contains(x)) added.Add(x);

            return (added, removed);
        }
    }
}

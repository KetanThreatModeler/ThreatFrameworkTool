using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.Model
{
    public class EntityDiff<T>
    {
        public List<T> Added { get; init; } = new();
        public List<T> Removed { get; init; } = new();
        public List<ModifiedEntity<T>> Modified { get; init; } = new();
    }
}

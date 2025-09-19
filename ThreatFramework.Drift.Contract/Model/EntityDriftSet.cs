using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.Model
{
    public class EntityDriftSet<T> where T : class
    {
        public List<T> Added { get; } = new();
        public List<T> Removed { get; } = new();
        public List<EntityDriftPair<T>> Modified { get; } = new();
    }
}

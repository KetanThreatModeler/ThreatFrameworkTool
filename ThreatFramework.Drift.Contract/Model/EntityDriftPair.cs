using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.Model
{
    public class EntityDriftPair<T> where T : class
    {
        public T Existing { get; set; } = default!;
        public T Updated { get; set; } = default!;
    }
}

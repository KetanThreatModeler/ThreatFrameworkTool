using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core;

namespace ThreatFramework.Drift.Contract.Model
{
    public class ModifiedEntity<T>
    {
        public T Entity { get; init; } = default!;
        public List<FieldChange> ModifiedFields { get; init; } = new();
    }
}

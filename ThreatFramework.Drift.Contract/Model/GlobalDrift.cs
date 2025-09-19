using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Drift.Contract.Model
{
    public class GlobalDrift
    {
        public EntityDiff<PropertyOption> PropertyOptions { get; init; } = new();
    }
}

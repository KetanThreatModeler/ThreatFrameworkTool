using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Drift.Contract.Model
{
    public class LibraryDrift
    {
        public Guid LibraryGuid { get; init; }
        public string LibraryName { get; init; } = string.Empty;

        public EntityDiff<Threat> Threats { get; init; } = new();
        public EntityDiff<Component> Components { get; init; } = new();
        public EntityDiff<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public EntityDiff<TestCase> TestCases { get; init; } = new();
        public EntityDiff<Property> Properties { get; init; } = new();
    }
}

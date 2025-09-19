using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Drift.Contract.Model
{
    public class EntityDriftReport
    {
        public EntityDriftSet<Threat> Threats { get; } = new();
        public EntityDriftSet<Component> Components { get; } = new();
        public EntityDriftSet<SecurityRequirement> SecurityRequirements { get; } = new();
        public EntityDriftSet<TestCase> TestCases { get; } = new();
        public EntityDriftSet<Property> Properties { get; } = new();
        public EntityDriftSet<PropertyOption> PropertyOptions { get; } = new();
        public EntityDriftSet<Library> Libraries { get; } = new();
    }
}

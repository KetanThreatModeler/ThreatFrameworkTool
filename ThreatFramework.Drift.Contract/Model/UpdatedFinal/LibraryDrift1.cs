using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Drift.Contract.MappingDriftService.Dto;

namespace ThreatModeler.TF.Drift.Contract.Model.UpdatedFinal
{
    public class LibraryDrift1
    {

        public Guid LibraryGuid { get; init; }
        public string LibraryName { get; init; } = string.Empty;
        public List<FieldChange> LibraryChanges { get; init; } = new();
        public ComponentDrift1 Components { get; init; } = new();
        public ThreatDrift1 Threats { get; init; } = new();
        public EntityDiff<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public EntityDiff<TestCase> TestCases { get; init; } = new();
        public EntityDiff<Property> Properties { get; init; } = new();
    }

    public class AddedLibrary1
    {
        public Library Library { get; init; }
        public List<AddedComponent1> Components { get; init; } = new();
        public List<AddedThreat1> Threats { get; init; } = new();
        public List<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public List<TestCase> TestCases { get; init; } = new();
        public List<Property> Properties { get; init; } = new();
    }

    public class DeletedLibrary1
    {
        public Guid LibraryGuid { get; init; }
        public string LibraryName { get; init; } = string.Empty;
        public List<DeletedComponent1> Components { get; init; } = new();
        public List<DeletedThreat1> Threats { get; init; } = new();
        public List<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public List<TestCase> TestCases { get; init; } = new();
        public List<Property> Properties { get; init; } = new();

    }

    public class ThreatDrift1
    {
        public List<AddedThreat1> Added { get; init; } = new();
        public List<DeletedThreat1> Deleted { get; init; } = new();
        public List<ModifiedThreat1> Modified { get; init; } = new();
    }

    public class AddedThreat1
    {
        public Threat Threat { get; init; }
        public List<SecurityRequirement> SecurityRequirementsAdded { get; init; } = new();
    }

    public class DeletedThreat1
    {
        public Threat Threat { get; init; }
        public List<SecurityRequirement> SecurityRequirementRemoved { get; init; } = new();
    }

    public class ModifiedThreat1
    {
        public Threat Threat { get; init; }
        public List<FieldChange> ChangedFields { get; set; } = new();
        public List<SecurityRequirement> SecurityRequirementAdded { get; init; } = new();
        public List<SecurityRequirement> SecurityRequirementRemoved { get; init; } = new();
    }
}

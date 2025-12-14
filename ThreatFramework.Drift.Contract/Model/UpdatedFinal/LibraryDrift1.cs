using ThreatFramework.Core;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatModeler.TF.Drift.Contract.Model.UpdatedFinal
{
    public class LibraryDrift1
    {

        public Library library { get; init; }
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
        public ThreatMappingCollection1 Added{ get; init; } = new();
    }

    public class DeletedThreat1
    {
        public Threat Threat { get; init; }
        public ThreatMappingCollection1 Removed { get; init; } = new();
    }

    public class ModifiedThreat1
    {
        public Threat Threat { get; init; }
        public List<FieldChange> ChangedFields { get; set; } = new();
        public ThreatMappingCollection1 Added { get; init; } = new();
        public ThreatMappingCollection1 Removed { get; init; } = new();
    }

    public class ThreatMappingCollection1
    {
        public List<SecurityRequirement> SecurityRequirement { get; init; } = new();
    }
}

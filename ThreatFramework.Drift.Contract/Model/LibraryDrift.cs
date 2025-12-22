using ThreatFramework.Core;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Core.Model.CoreEntities;

namespace ThreatModeler.TF.Drift.Contract.Model
{
    public class LibraryDrift
    {

        public Library library { get; init; }
        public List<FieldChange> LibraryChanges { get; init; } = new();
        public ComponentDrift Components { get; init; } = new();
        public ThreatDrift Threats { get; init; } = new();
        public EntityDiff<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public EntityDiff<TestCase> TestCases { get; init; } = new();
        public EntityDiff<Property> Properties { get; init; } = new();
        public ResourceTypeValueDrift ResourceTypeValues { get; init; } = new();
    }

    public class AddedLibrary
    {
        public Library Library { get; init; }
        public List<AddedComponent> Components { get; init; } = new();
        public List<AddedThreat> Threats { get; init; } = new();
        public List<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public List<TestCase> TestCases { get; init; } = new();
        public List<Property> Properties { get; init; } = new();
        public List<AddedResourceTypeValue> ResourceTypeValues { get; init; } = new();
    }

    public class DeletedLibrary
    {
        public Library Library { get; init; } = new Library();
        public List<DeletedComponent> Components { get; init; } = new();
        public List<DeletedThreat> Threats { get; init; } = new();
        public List<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public List<TestCase> TestCases { get; init; } = new();
        public List<Property> Properties { get; init; } = new();
        public List<DeletedResourceTypeValue> ResourceTypeValues { get; init; } = new();
    }

    public class ThreatDrift
    {
        public List<AddedThreat> Added { get; init; } = new();
        public List<DeletedThreat> Removed { get; init; } = new();
        public List<ModifiedThreat> Modified { get; init; } = new();
    }

    public class AddedThreat
    {
        public Threat Threat { get; init; }
        public List<SecurityRequirement> SecurityRequirements{ get; init; } = new();
    }

    public class DeletedThreat
    {
        public Threat Threat { get; init; }
        public List<SecurityRequirement> SecurityRequirements { get; init; } = new();
    }

    public class ModifiedThreat
    {
        public Threat Threat { get; init; }
        public List<FieldChange> ChangedFields { get; set; } = new();
        public List<SecurityRequirement> SecurityRequirementAdded { get; init; } = new();
        public List<SecurityRequirement> SecurityRequirementRemoved { get; init; } = new();
    }

    public class AddedResourceTypeValue
    {
        public ResourceTypeValues ResourceTypeValue { get; init; } = new();
        public List<ResourceTypeValueRelationship> Relationships { get; init; } = new();
    }

    public class DeletedResourceTypeValue
    {
        public ResourceTypeValues ResourceTypeValue { get; init; }
        public List<ResourceTypeValueRelationship> Relationships { get; init; } = new();
    }


    public class ResourceTypeValueDrift
    {
        public List<AddedResourceTypeValue> Added { get; init; } = new();
        public List<DeletedResourceTypeValue> Removed { get; init; } = new();
        public List<ModifiedResourceTypeValue> Modified { get; init; } = new();
    }

    public class ModifiedResourceTypeValue
    {
        public ResourceTypeValues ResourceTypeValue { get; init; }
        public List<FieldChange> ChangedFields { get; set; } = new();
        public List<ResourceTypeValueRelationship> AddedRelationships { get; init; } = new();
        public List<ResourceTypeValueRelationship> RemovedRelationships { get; init; } = new();
        public List<ModifiedResourceTypeValueRelationship> ModifiedRelationships { get; init; } = new();
    }

    public class ModifiedResourceTypeValueRelationship
    {
        public ResourceTypeValueRelationship Relationship { get; init; }
        public List<FieldChange> ChangedFields { get; set; } = new();
    }
}
using ThreatFramework.Core;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Core.Model.CoreEntities;

namespace ThreatModeler.TF.Drift.Contract.Dto
{
    public class LibraryDriftDto
    {
        public Guid LibraryGuid { get; init; }
        public List<FieldChange> LibraryChanges { get; init; } = new();
        public ComponentDriftDto Components { get; init; } = new();
        public ThreatDriftDto Threats { get; set; } = new();
        public EntityDiff<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public EntityDiff<TestCase> TestCases { get; init; } = new();
        public EntityDiff<Property> Properties { get; init; } = new();
        public EntityDiff<ResourceTypeValues> ResourceTypeValues { get; init; } = new();
        public EntityDiff<ResourceTypeValueRelationship> ResourceTypeValueRelationships { get; init; } = new();
    }


    public class ComponentDriftDto
    {
        public List<AddedComponentDto> Added { get; init; } = new();
        public List<DeletedComponentDto> Deleted { get; init; } = new();
        public List<ModifiedComponentDto> Modified { get; init; } = new();
    }

    // Reusable mapping collection for composition
    public class ComponentMappingCollectionDto
    {
        public List<SRMappingDto> SecurityRequirements { get; set; } = new();
        public List<ThreatSRMappingDto> ThreatSRMappings { get; set; } = new();
        public List<PropertyThreatSRMappingDto> PropertyThreatSRMappings { get; set; } = new();
    }

    public class AddedComponentDto
    {
        public Component Component { get; set; } = null!;

        // All mappings for this added component (implicitly "added" mappings)
        public ComponentMappingCollectionDto Mappings { get; set; } = new();
    }

    public class DeletedComponentDto
    {
        public Component Component { get; set; } = null!;

        // All mappings for this deleted component (implicitly "deleted" mappings)
        public ComponentMappingCollectionDto Mappings { get; set; } = new();
    }

    public class ModifiedComponentDto
    {
        public Component Component { get; set; } = null!;

        // Reuse existing ModifiedEntity structure for field changes
        public List<FieldChange> ChangedFields { get; set; } = new();

        // Granular mapping changes for this modified component
        public ComponentMappingCollectionDto MappingsAdded { get; set; } = new();
        public ComponentMappingCollectionDto MappingsRemoved { get; set; } = new();
    }
}

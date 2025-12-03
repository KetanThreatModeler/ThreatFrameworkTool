using ThreatFramework.Core;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;
using ThreatModeler.TF.Drift.Contract.MappingDriftService.Dto;

namespace ThreatFramework.Drift.Contract.Model
{
    public class LibraryDrift
    {
        public Guid LibraryGuid { get; init; }
        public string LibraryName { get; init; } = string.Empty;
        public List<FieldChange> LibraryChanges { get; init; } = new();
        public ComponentDrift Components { get; init; } = new();
        public EntityDiff<Threat> Threats { get; init; } = new();
        public EntityDiff<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public EntityDiff<TestCase> TestCases { get; init; } = new();
        public EntityDiff<Property> Properties { get; init; } = new();
    }

    public class ComponentDrift
    {
        public List<AddedComponent> Added { get; init; } = new();
        public List<DeletedComponent> Deleted { get; init; } = new();
        public List<ModifiedComponent> Modified { get; init; } = new();
    }

    // Reusable mapping collection for composition
    public class ComponentMappingCollection
    {
        public List<SRMappingDto> SecurityRequirements { get; set; } = new();
        public List<ThreatSRMapping> ThreatSRMappings { get; set; } = new();
        public List<PropertyThreatSRMapping> PropertyThreatSRMappings { get; set; } = new();
    }

    public class AddedComponent
    {
        public Component Component { get; set; } = null!;

        // All mappings for this added component (implicitly "added" mappings)
        public ComponentMappingCollection Mappings { get; set; } = new();
    }

    public class DeletedComponent
    {
        public Component Component { get; set; } = null!;

        // All mappings for this deleted component (implicitly "deleted" mappings)
        public ComponentMappingCollection Mappings { get; set; } = new();
    }

    public class ModifiedComponent
    {
        public Component Component { get; set; } = null!;

        // Reuse existing ModifiedEntity structure for field changes
        public List<FieldChange> ChangedFields { get; set; } = new();

        // Granular mapping changes for this modified component
        public ComponentMappingCollection MappingsAdded { get; set; } = new();
        public ComponentMappingCollection MappingsRemoved { get; set; } = new();
    }
}

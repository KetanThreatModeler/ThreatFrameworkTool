using System.Text;
using ThreatFramework.Core;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Global;

namespace ThreatModeler.TF.Drift.Contract.Dto
{
    public class TMFrameworkDriftDto
    {
        public List<LibraryDriftDto> ModifiedLibraries { get; init; } = new();
        public List<AddedLibraryDto> AddedLibraries { get; init; } = new();
        public List<DeletedLibraryDto> DeletedLibraries { get; init; } = new();
        public GlobalDriftDto Global { get; set; } = new ();
    }

    public class AddedLibraryDto
    {
        public Library Library { get; init; }
        public List<AddedComponentDto> Components { get; init; } = new();
        public List<AddedThreatDto> Threats { get; init; } = new();
        public List<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public List<TestCase> TestCases { get; init; } = new();
        public List<Property> Properties { get; init; } = new();
    }

    public class DeletedLibraryDto
    {
        public Guid LibraryGuid { get; init; }
        public string LibraryName { get; init; } = string.Empty;
        public List<DeletedComponentDto> Components { get; init; } = new();
        public List<RemovedThreatDto> Threats { get; init; } = new();
        public List<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public List<TestCase> TestCases { get; init; } = new();
        public List<Property> Properties { get; init; } = new();
    }

    public class GlobalDriftDto
    {
        public EntityDiff<PropertyOption> PropertyOptions { get; init; } = new();
        public EntityDiff<PropertyType> PropertyTypes { get; init; } = new();
        public EntityDiff<ComponentType> ComponentTypes { get; init; } = new();
    }


    public class ThreatDriftDto
    {
        public List<AddedThreatDto> Added { get; init; } = new();
        public List<RemovedThreatDto> Removed { get; init; } = new();
        public List<ModifiedThreatDto> Modified { get; init; } = new();
    }

    public class AddedThreatDto
    {
        public Threat Threat { get; init; }
        public ThreatMappingCollectionDto Mappings { get; init; }
    }

    public class ModifiedThreatDto
    {
        public Threat Threat { get; init; }
        public List<FieldChange> ChangedFields { get; set; } = new();

        // Granular mapping changes for this modified component
        public ThreatMappingCollectionDto MappingsAdded { get; set; } = new();
        public ThreatMappingCollectionDto MappingsRemoved { get; set; } = new();
    }

    public class RemovedThreatDto
    {
        public Threat Threat { get; init; }
        public ThreatMappingCollectionDto Mappings { get; set; }
    }

    public class ThreatMappingCollectionDto
    {
        public List<SRMappingDto> SecurityRequirements { get; set; } = new();
    }

}

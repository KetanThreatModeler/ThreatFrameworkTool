using System.Text;
using ThreatFramework.Core;
using ThreatFramework.Core.CoreEntities;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Drift.Contract.MappingDriftService.Dto;

namespace ThreatFramework.Drift.Contract.Model
{
    public class TMFrameworkDrift
    {
        public List<LibraryDrift> ModifiedLibraries { get; init; } = new();
        public List<AddedLibrary> AddedLibraries { get; init; } = new();
        public List<DeletedLibrary> DeletedLibraries { get; init; } = new();
        public GlobalDrift Global { get; set; }
    }

    public class AddedLibrary
    {
        public Library Library { get; init; }
        public List<AddedComponent> Components { get; init; } = new();
        public List<AddedThreat> Threats { get; init; } = new();
        public List<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public List<TestCase> TestCases { get; init; } = new();
        public List<Property> Properties { get; init; } = new();
    }

    public class DeletedLibrary
    {
        public Guid LibraryGuid { get; init; }
        public string LibraryName { get; init; } = string.Empty;
        public List<DeletedComponent> Components { get; init; } = new();
        public List<RemovedThreat> Threats { get; init; } = new();
        public List<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public List<TestCase> TestCases { get; init; } = new();
        public List<Property> Properties { get; init; } = new();
    }

    public class GlobalDrift
    {
        public EntityDiff<PropertyOption> PropertyOptions { get; init; } = new();
        public EntityDiff<PropertyType> PropertyTypes { get; init; } = new();
        public EntityDiff<ComponentType> ComponentTypes { get; init; } = new();
    }


    public class ThreatDrift
    {
        public List<AddedThreat> Added { get; init; } = new();
        public List<RemovedThreat> Removed { get; init; } = new();
        public List<ModifiedThreat> Modified { get; init; } = new();
    }

    public class AddedThreat
    {
        public Threat Threat { get; init; }
        public ThreatMappingCollection Mappings { get; init; }
    }

    public class ModifiedThreat
    {
        public Threat Threat { get; init; }
        public List<FieldChange> ChangedFields { get; set; } = new();

        // Granular mapping changes for this modified component
        public ThreatMappingCollection MappingsAdded { get; set; } = new();
        public ThreatMappingCollection MappingsRemoved { get; set; } = new();
    }

    public class RemovedThreat
    {
        public Threat Threat { get; init; }
        public ThreatMappingCollection Mappings { get; set; }
    }

    public class ThreatMappingCollection
    {
        public List<SRMappingDto> SecurityRequirements { get; set; } = new();
    }

}

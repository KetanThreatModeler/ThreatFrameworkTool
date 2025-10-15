using ThreatFramework.Drift.Contract.MappingDriftService.Model;

namespace ThreatFramework.Drift.Contract.MappingDriftService.Dto
{
    public class ComponentMappingDriftDto
    {
        public Guid ComponentGuid { get; init; }
        public List<Guid> SecurityRequirementsAdded { get; init; } = new List<Guid>();
        public List<Guid> SecurityRequirementsRemoved { get; init; } = new List<Guid>();
        public List<ThreatSRMapping> MappingsAdded { get; init; } = new List<ThreatSRMapping>();
        public List<ThreatSRMapping> MappingsRemoved { get; init; } = new List<ThreatSRMapping>();
        public List<PropertyThreatSRMapping> PropertyThreatSRMappingsAdded { get; init; } = new List<PropertyThreatSRMapping>();
        public List<PropertyThreatSRMapping> PropertyThreatSRMappingsRemoved { get; init; } = new List<PropertyThreatSRMapping>();
    }
}

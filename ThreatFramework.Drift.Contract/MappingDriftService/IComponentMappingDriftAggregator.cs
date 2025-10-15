using ThreatFramework.Drift.Contract.MappingDriftService.Dto;

namespace ThreatFramework.Drift.Contract.MappingDriftService
{
    public interface IComponentMappingDriftAggregator
    {
        List<ComponentMappingDriftDto> Compose(
            IEnumerable<ComponentPropertyThSrDiffDto> propertyDrift,
            IEnumerable<ComponentThreatSRDriftDto> componentThreatDrift,
            IEnumerable<ComponentSRDriftDto> componentSrDrift);
    }
}

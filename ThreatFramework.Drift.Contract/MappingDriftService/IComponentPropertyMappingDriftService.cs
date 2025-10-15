using ThreatFramework.Drift.Contract.MappingDriftService.Dto;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;

namespace ThreatFramework.Drift.Contract.MappingDriftService
{
    public interface IComponentPropertyMappingDriftService
    {
        List<ComponentPropertyThSrDiffDto> ComputeDrift(ComponentPropertyGraph sourceA, ComponentPropertyGraph sourceB);
    }
}

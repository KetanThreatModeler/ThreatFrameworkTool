using ThreatFramework.Drift.Contract.MappingDriftService.Dto;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;

namespace ThreatFramework.Drift.Contract.MappingDriftService
{
    public interface IComponentSRDriftService
    {
        List<ComponentSRDriftDto> ComputeDrift(ComponentSRGraph sourceA, ComponentSRGraph sourceB);
    }
}

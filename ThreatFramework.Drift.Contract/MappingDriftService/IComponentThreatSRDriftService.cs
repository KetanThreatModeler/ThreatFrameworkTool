using ThreatFramework.Drift.Contract.MappingDriftService.Dto;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;

namespace ThreatFramework.Drift.Contract.MappingDriftService
{
    public interface IComponentThreatSRDriftService
    {
        List<ComponentThreatSRDriftDto> ComputeDrift(
            ComponentThreatSRGraph sourceA,
            ComponentThreatSRGraph sourceB);
    }
}

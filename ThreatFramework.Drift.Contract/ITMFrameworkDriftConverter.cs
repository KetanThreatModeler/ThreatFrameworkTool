using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Drift.Contract.Model;

namespace ThreatModeler.TF.Drift.Contract
{
    public interface ITMFrameworkDriftConverter
    {
        Task<TMFrameworkDrift1> ConvertAsync(TMFrameworkDriftDto source);
    }
}

using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Drift.Contract.Model.UpdatedFinal;

namespace ThreatModeler.TF.Drift.Contract
{
    public interface ITMFrameworkDriftConverter
    {
        Task<TMFrameworkDrift1> ConvertAsync(TMFrameworkDriftDto source);
    }
}

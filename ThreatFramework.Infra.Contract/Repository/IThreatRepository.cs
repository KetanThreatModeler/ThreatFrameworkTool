using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IThreatRepository
    {
        Task<IEnumerable<Threat>> GetThreatsByLibraryIdAsync(IEnumerable<Guid> guids);
        Task<IEnumerable<Threat>> GetReadOnlyThreatsAsync();
    }
}

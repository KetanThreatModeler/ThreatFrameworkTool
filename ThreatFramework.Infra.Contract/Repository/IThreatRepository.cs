using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface IThreatRepository
    {
        Task<IEnumerable<Threat>> GetThreatsByLibraryIdAsync(IEnumerable<Guid> guids);
        Task<IEnumerable<Threat>> GetReadOnlyThreatsAsync();
    }
}

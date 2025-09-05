using ThreatFramework.Core.Models.ComponentMapping;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{

    public interface IComponentThreatSecurityRequirementMappingRepository
    {
        Task<IEnumerable<ComponentThreatSecurityRequirementMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids);
        Task<IEnumerable<ComponentThreatSecurityRequirementMapping>> GetReadOnlyMappingsAsync();
    }
}

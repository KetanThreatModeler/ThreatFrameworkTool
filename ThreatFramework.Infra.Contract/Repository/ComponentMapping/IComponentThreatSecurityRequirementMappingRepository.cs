using ThreatFramework.Core.ComponentMapping;

namespace ThreatFramework.Infra.Contract.Repository
{

    public interface IComponentThreatSecurityRequirementMappingRepository
    {
        Task<IEnumerable<ComponentThreatSecurityRequirementMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids);
        Task<IEnumerable<ComponentThreatSecurityRequirementMapping>> GetReadOnlyMappingsAsync();
    }
}

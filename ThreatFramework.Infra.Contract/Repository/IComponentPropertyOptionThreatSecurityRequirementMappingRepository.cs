using ThreatFramework.Core.PropertyMapping;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IComponentPropertyOptionThreatSecurityRequirementMappingRepository
    {
        Task<IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping>> GetMappingsByLibraryGuidAsync(IEnumerable<Guid> libraryGuids);
        Task<IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping>> GetReadOnlyMappingsAsync();
    }
}

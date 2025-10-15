using ThreatFramework.Core.PropertyMapping;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IComponentPropertyMappingRepository
    {
        Task<IEnumerable<ComponentPropertyMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids);
        Task<IEnumerable<ComponentPropertyMapping>> GetReadOnlyMappingsAsync();
    }
}

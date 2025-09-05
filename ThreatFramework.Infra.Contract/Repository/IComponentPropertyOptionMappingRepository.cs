using ThreatFramework.Core.Models.PropertyMapping;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IComponentPropertyOptionMappingRepository
    {

        Task<IEnumerable<ComponentPropertyOptionMapping>> GetMappingsByLibraryGuidAsync(Guid libraryGuid);
        Task<IEnumerable<ComponentPropertyOptionMapping>> GetReadOnlyMappingsAsync();
    }
}

using ThreatFramework.Core.Models.PropertyMapping;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IComponentPropertyOptionThreatMappingRepository
    {
        Task<IEnumerable<ComponentPropertyOptionThreatMapping>> GetMappingsByLibraryGuidAsync(IEnumerable<Guid> libraryGuids);
        Task<IEnumerable<ComponentPropertyOptionThreatMapping>> GetReadOnlyMappingsAsync();
    }
}

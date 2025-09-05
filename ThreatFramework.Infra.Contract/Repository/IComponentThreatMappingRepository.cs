using ThreatFramework.Core.Models.ComponentMapping;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IComponentThreatMappingRepository
    {
        Task<IEnumerable<ComponentThreatMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids);
        Task<IEnumerable<ComponentThreatMapping>> GetReadOnlyMappingsAsync();
    }
}

using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IComponentRepository
    {
        Task<IEnumerable<Component>> GetComponentsByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Component>> GetReadOnlyComponentsAsync();
    }
}

using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface IComponentRepository
    {
        Task<IEnumerable<Component>> GetComponentsByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Component>> GetReadOnlyComponentsAsync();
    }
}

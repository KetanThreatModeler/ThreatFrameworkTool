using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IComponentRepository
    {
        Task<IEnumerable<Component>> GetComponentsByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Component>> GetReadOnlyComponentsAsync();
        Task<IEnumerable<Guid>> GetGuidsAsync();
        Task<IEnumerable<(Guid ComponentGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync();
        Task<IEnumerable<(Guid ComponentGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds);
    }
}

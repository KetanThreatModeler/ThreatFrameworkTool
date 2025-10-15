using ThreatFramework.Core.Cache;
using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface ILibraryRepository
    {
        Task<IEnumerable<LibraryCache>> GetLibrariesCacheAsync();
        Task<IEnumerable<Library>> GetLibrariesByGuidsAsync(IEnumerable<Guid> guids);
        Task<IEnumerable<Library>> GetReadonlyLibrariesAsync();
        Task<IEnumerable<Guid>> GetLibraryGuidsAsync();
    }
}

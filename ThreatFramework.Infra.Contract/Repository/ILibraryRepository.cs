using ThreatFramework.Core.Models.Cache;
using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface ILibraryRepository
    {
        Task<IEnumerable<LibraryCache>> GetLibrariesCacheAsync();
        Task<IEnumerable<Library>> GetLibrariesByGuidsAsync(IEnumerable<Guid> guids);
        Task<IEnumerable<Library>> GetReadonlyLibrariesAsync();
    }
}

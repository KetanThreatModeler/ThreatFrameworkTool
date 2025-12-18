using ThreatFramework.Core.Cache;
using ThreatModeler.TF.Core.Model.CoreEntities;

namespace ThreatModeler.TF.Infra.Contract.Repository.CoreEntities
{
    public interface ILibraryRepository
    {
        Task<IEnumerable<LibraryCache>> GetLibrariesCacheAsync();
        Task<IEnumerable<Library>> GetLibrariesByGuidsAsync(IEnumerable<Guid> guids);
        Task<IEnumerable<Library>> GetReadonlyLibrariesAsync();
        Task<IEnumerable<Guid>> GetLibraryGuidsAsync();
        Task<IEnumerable<(Guid LibGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<(Guid LibGuid, Guid LibraryGuid)>> GetLibraryGuidsWithLibGuidAsync();
    }
}

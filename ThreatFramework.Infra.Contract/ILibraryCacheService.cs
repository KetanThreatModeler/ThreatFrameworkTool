using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract
{
    public interface ILibraryCacheService
    {
        Task<Dictionary<int, Guid>> GetIdToGuidLookupAsync();

        Task<HashSet<Guid>> GetReadonlyLibraryGuidsAsync();

        Task<HashSet<int>> GetIdsFromGuid(IEnumerable<Guid> guids);

        Task<HashSet<int>> GetReadOnlyLibraryIdAsync();

        Task<Guid> GetGuidByIdAsync(int libraryId);

        Task<bool> IsLibraryReadonlyAsync(Guid libraryGuid);

        Task RefreshCacheAsync();

        Task ClearCacheAsync();
    }
}

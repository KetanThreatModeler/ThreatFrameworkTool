using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.Index;

namespace ThreatModeler.TF.Infra.Contract.Index.Client
{
    public interface IClientGuidIndexService
    {
        Task GenerateAsync(string? outputPath = null);
        Task GenerateForLibraryAsync(IEnumerable<Guid> libIds, string? outputPath = null);
        Task RefreshAsync(string? path = null);
        int GetInt(Guid guid);

        Task<(int, int)> GetIntIdOfEntityAndLibIdByGuidAsync(Guid guid);

        Guid GetGuid(int id);
        IReadOnlyCollection<int> GetIdsByLibraryAndType(Guid libraryId, EntityType entityType);

        // Convenience methods
        IReadOnlyCollection<int> GetComponentIds(Guid libraryId);
        IReadOnlyCollection<int> GetThreatIds(Guid libraryId);
        IReadOnlyCollection<int> GetSecurityRequirementIds(Guid libraryId);
        IReadOnlyCollection<int> GetPropertyIds(Guid libraryId);
        IReadOnlyCollection<int> GetTestCaseIds(Guid libraryId);
    }
}

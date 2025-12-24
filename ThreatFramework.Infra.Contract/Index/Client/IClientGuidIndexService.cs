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
        Task GenerateAsync();
        Task GenerateForLibraryAsync(IEnumerable<Guid> libIds);
        Task RefreshAsync();
        Task<int> GetIntAsync(Guid guid);
        Task<(int entityId, int libId)> GetIntIdOfEntityAndLibIdByGuidAsync(Guid guid);
        Task<Guid> GetGuidAsync(int id);
        Task<IReadOnlyCollection<int>> GetIdsByLibraryAndTypeAsync(Guid libraryId, EntityType entityType);
        Task<IReadOnlyCollection<int>> GetComponentIdsAsync(Guid libraryId);
        Task<IReadOnlyCollection<int>> GetThreatIdsAsync(Guid libraryId);
        Task<IReadOnlyCollection<int>> GetSecurityRequirementIdsAsync(Guid libraryId);
        Task<IReadOnlyCollection<int>> GetPropertyIdsAsync(Guid libraryId);
        Task<IReadOnlyCollection<int>> GetTestCaseIdsAsync(Guid libraryId);
    }
}

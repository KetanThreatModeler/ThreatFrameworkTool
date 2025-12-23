using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.Index;
using ThreatModeler.TF.Infra.Contract.Index.Client;

namespace ThreatModeler.TF.Infra.Implmentation.Index.Client
{
    public class ClientGuidIndexService : IClientGuidIndexService
    {
        public Task GenerateAsync(string? outputPath = null)
        {
            throw new NotImplementedException();
        }

        public Task GenerateForLibraryAsync(IEnumerable<Guid> libIds, string? outputPath = null)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<int> GetComponentIds(Guid libraryId)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int id)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<int> GetIdsByLibraryAndType(Guid libraryId, EntityType entityType)
        {
            throw new NotImplementedException();
        }

        public int GetInt(Guid guid)
        {
            throw new NotImplementedException();
        }

        public Task<(int, int)> GetIntIdOfEntityAndLibIdByGuidAsync(Guid guid)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<int> GetPropertyIds(Guid libraryId)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<int> GetSecurityRequirementIds(Guid libraryId)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<int> GetTestCaseIds(Guid libraryId)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<int> GetThreatIds(Guid libraryId)
        {
            throw new NotImplementedException();
        }

        public Task RefreshAsync(string? path = null)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.Index;

namespace ThreatModeler.TF.Infra.Contract.Index.Client
{
    public interface IClientGuidSource
    {
        Task<IEnumerable<EntityIdentifier>> GetAllGuidsWithTypeAsyncForClient();
        Task<IEnumerable<EntityIdentifier>> GetGuidsWithTypeByLibraryIdsAsyncForClient(IEnumerable<Guid> libraryIds);
    }
}

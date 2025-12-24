using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Infra.Contract.Index.Common
{
    public interface IGuidIndexService
    {
        Task RefreshAsync();
        Task<int> GetIntAsync(Guid guid);
        Task<Guid> GetGuidAsync(int id);

    }
}

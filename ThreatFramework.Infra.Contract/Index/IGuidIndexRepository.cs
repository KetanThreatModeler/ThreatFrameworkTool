using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract.Index
{
    public interface IGuidIndexRepository
    {
        Task<IEnumerable<GuidIndex>> LoadAsync(string path, CancellationToken ct = default);
    }
}

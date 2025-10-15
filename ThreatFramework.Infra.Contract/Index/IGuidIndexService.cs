using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract.Index
{
    public interface IGuidIndexService
    {
        Task GenerateAsync(string? outputPath = null);

        Task RefreshAsync(string? path = null);

        int GetInt(Guid guid);
    }
}

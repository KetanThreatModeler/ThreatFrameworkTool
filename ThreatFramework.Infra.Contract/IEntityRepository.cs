using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract
{
    public interface IEntityRepository<T>
    {
        Task<IReadOnlyCollection<T>> LoadAllAsync(
            string rootFolder,
            IEnumerable<string>? onlyRelativePaths = null,
            CancellationToken ct = default);
    }
}

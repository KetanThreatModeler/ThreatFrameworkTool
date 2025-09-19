using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatFramework.Drift.Contract
{
    public interface IYamlDriftBuilder
    {
        Task<EntityDriftReport> BuildAsync(YamlFilesDriftReport request, CancellationToken cancellationToken = default);
    }
}

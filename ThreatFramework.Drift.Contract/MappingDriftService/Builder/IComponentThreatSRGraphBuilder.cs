using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.ComponentMapping;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;

namespace ThreatFramework.Drift.Contract.MappingDriftService.Builder
{
    public interface IComponentThreatSRGraphBuilder
    {
        ComponentThreatSRGraph Build(
            IEnumerable<ComponentThreatSecurityRequirementMapping> srRows,
            IEnumerable<ComponentThreatMapping> threatRows);
    }
}

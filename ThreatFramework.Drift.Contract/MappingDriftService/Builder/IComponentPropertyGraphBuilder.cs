using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Drift.Contract.MappingDriftService.Model;

namespace ThreatFramework.Drift.Contract.MappingDriftService.Builder
{
    public interface IComponentPropertyGraphBuilder
    {
        ComponentPropertyGraph Build(
            IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping> srRows,
            IEnumerable<ComponentPropertyOptionThreatMapping> threatRows,
            IEnumerable<ComponentPropertyOptionMapping> optionRows,
            IEnumerable<ComponentPropertyMapping> propertyRows);
    }
}

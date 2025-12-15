using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Core.Model.Global;
using ThreatModeler.TF.Drift.Contract.Model;

namespace ThreatModeler.TF.Drift.Contract
{
    public interface IYamlRouter
    {
        Task<Library> GetLibraryByGuidAsync(Guid guid, DriftSource source);
        Task<Component> GetComponentByGuidAsync(Guid guid, DriftSource source);
        Task<Threat> GetThreatByGuidAsync(Guid guid, DriftSource source);
        Task<SecurityRequirement> GetSecurityRequirementByGuidAsync(Guid guid, DriftSource source);
        Task<Property> GetPropertyByGuidAsync(Guid guid, DriftSource source);
        Task<TestCase> GetTestCaseByGuidAsync(Guid guid, DriftSource source);
        Task<PropertyOption> GetPropertyOptionByGuidAsync(Guid guid, DriftSource source);
    }
}

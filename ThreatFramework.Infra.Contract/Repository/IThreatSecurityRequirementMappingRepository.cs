using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.ThreatMapping;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IThreatSecurityRequirementMappingRepository
    {
        Task<IEnumerable<ThreatSecurityRequirementMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids);
        Task<IEnumerable<ThreatSecurityRequirementMapping>> GetReadOnlyMappingsAsync();

    }
}

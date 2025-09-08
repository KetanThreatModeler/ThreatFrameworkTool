using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.ComponentMapping;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IThreatSecurityRequirementMappingRepository
    {
        Task<IEnumerable<ThreatSecurityRequirementMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids);
        Task<IEnumerable<ThreatSecurityRequirementMapping>> GetReadOnlyMappingsAsync();
    }
}

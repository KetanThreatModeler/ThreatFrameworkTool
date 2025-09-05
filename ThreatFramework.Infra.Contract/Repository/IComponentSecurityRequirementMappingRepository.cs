using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Models.ComponentMapping;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IComponentSecurityRequirementMappingRepository
    {
        Task<IEnumerable<ComponentSecurityRequirementMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids);
        Task<IEnumerable<ComponentSecurityRequirementMapping>> GetReadOnlyMappingsAsync();
    }
}

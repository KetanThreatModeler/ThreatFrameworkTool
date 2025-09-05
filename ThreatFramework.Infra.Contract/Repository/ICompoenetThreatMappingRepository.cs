using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.Models.ComponentMapping;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IComponentThreatMappingRepository
    {
        Task<IEnumerable<ComponentThreatMapping>> GetMappingsByLibraryIdAsync(IEnumerable<Guid> libraryGuids);
        Task<IEnumerable<ComponentThreatMapping>> GetReadOnlyMappingsAsync();
    }
}

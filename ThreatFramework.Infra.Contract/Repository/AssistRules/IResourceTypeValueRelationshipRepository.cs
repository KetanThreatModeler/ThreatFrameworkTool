using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.AssistRules;

namespace ThreatModeler.TF.Infra.Contract.Repository.AssistRules
{
    public interface IResourceTypeValueRelationshipRepository
    {
        Task<IEnumerable<ResourceTypeValueRelationship>> GetAllAsync();

        Task<IEnumerable<ResourceTypeValueRelationship>> GetByLibraryGuidsAsync(List<Guid> libraryGuids);

        Task<IEnumerable<ResourceTypeValueRelationship>> GetBySourceResourceTypeValueAsync(
            string sourceResourceTypeValue);
    }
}

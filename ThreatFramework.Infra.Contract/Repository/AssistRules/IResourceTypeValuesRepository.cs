using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.AssistRules;

namespace ThreatModeler.TF.Infra.Contract.Repository.AssistRules
{
    public interface IResourceTypeValuesRepository
    {
        Task<IEnumerable<ResourceTypeValues>> GetAllAsync();

        Task<IEnumerable<ResourceTypeValues>> GetByLibraryIdAsync(Guid libraryId);

        Task<ResourceTypeValues> GetByResourceTypeValueAsync(string resourceTypeValue);
    }
}

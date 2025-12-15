using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Model.AssistRules;

namespace ThreatModeler.TF.Infra.Contract.Repository.AssistRules
{
    public interface IRelationshipRepository
    {
        Task<IEnumerable<Relationship>> GetAllRelationshipsAsync();
        Task<IEnumerable<Guid>> GetAllGuidsAsync();
    }
}

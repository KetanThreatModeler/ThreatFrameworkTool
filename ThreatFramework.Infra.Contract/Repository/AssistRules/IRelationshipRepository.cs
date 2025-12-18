using ThreatModeler.TF.Core.Model.AssistRules;

namespace ThreatModeler.TF.Infra.Contract.Repository.AssistRules
{
    public interface IRelationshipRepository
    {
        Task<IEnumerable<Relationship>> GetAllRelationshipsAsync();
        Task<IEnumerable<Guid>> GetAllGuidsAsync();
    }
}

using ThreatModeler.TF.Core.Model.Global;

namespace ThreatModeler.TF.Infra.Contract.Repository.Global
{
    public interface IPropertyOptionRepository
    {
        Task<IEnumerable<PropertyOption>> GetAllPropertyOptionsAsync();
        Task<IEnumerable<Guid>> GetAllPropertyOptionGuidsAsync();
    }
}

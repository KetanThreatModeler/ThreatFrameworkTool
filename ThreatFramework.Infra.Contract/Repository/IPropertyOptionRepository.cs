using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IPropertyOptionRepository
    {
        Task<IEnumerable<PropertyOption>> GetAllPropertyOptionsAsync();
    }
}

using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IPropertyOptionRepository
    {
        Task<IEnumerable<PropertyOption>> GetAllPropertyOptionsAsync();
    }
}

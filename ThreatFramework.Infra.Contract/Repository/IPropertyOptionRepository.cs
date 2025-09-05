using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface IPropertyOptionRepository
    {
        Task<IEnumerable<PropertyOption>> GetAllPropertyOptionsAsync();
    }
}

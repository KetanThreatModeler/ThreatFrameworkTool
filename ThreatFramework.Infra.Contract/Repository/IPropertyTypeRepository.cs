using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IPropertyTypeRepository
    {
        Task<IEnumerable<PropertyType>> GetAllPropertyTypesAsync();
    }
}

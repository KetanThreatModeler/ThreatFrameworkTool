using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface IPropertyTypeRepository
    {
        Task<IEnumerable<PropertyType>> GetAllPropertyTypesAsync();
    }
}

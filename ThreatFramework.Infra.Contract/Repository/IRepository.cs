using ThreatFramework.Core.Models;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface IThreatRepository
    {
        Task<IEnumerable<Threat>> GetAllThreatsAsync();
    }

    public interface IComponentRepository
    {
        Task<IEnumerable<Component>> GetAllComponentsAsync();
    }

    public interface ILibraryRepository
    {
        Task<IEnumerable<Library>> GetAllLibrariesAsync();
    }

    public interface ISecurityRequirementRepository
    {
        Task<IEnumerable<SecurityRequirement>> GetAllSecurityRequirementsAsync();
    }

    public interface IPropertyRepository
    {
        Task<IEnumerable<Property>> GetAllPropertiesAsync();
    }

    public interface IPropertyTypeRepository
    {
        Task<IEnumerable<PropertyType>> GetAllPropertyTypesAsync();
    }

    public interface IPropertyOptionRepository
    {
        Task<IEnumerable<PropertyOption>> GetAllPropertyOptionsAsync();
    }

    public interface ITestcaseRepository
    {
        Task<IEnumerable<TestCase>> GetAllTestcasesAsync();
    }

    public interface IComponentPropertyMappingRepository
    {
        Task<IEnumerable<ComponentPropertyMapping>> GetAllComponentPropertyMappingsAsync();
    }

    public interface IComponentPropertyOptionMappingRepository
    {
        Task<IEnumerable<ComponentPropertyOptionMapping>> GetAllComponentPropertyOptionMappingsAsync();
    }

    public interface IComponentPropertyOptionThreatMappingRepository
    {
        Task<IEnumerable<ComponentPropertyOptionThreatMapping>> GetAllComponentPropertyOptionThreatMappingsAsync();
    }

    public interface IComponentPropertyOptionThreatSecurityRequirementMappingRepository
    {
        Task<IEnumerable<ComponentPropertyOptionThreatSecurityRequirementMapping>> GetAllComponentPropertyOptionThreatSecurityRequirementMappingsAsync();
    }

    public interface IComponentSecurityRequirementMappingRepository
    {
        Task<IEnumerable<ComponentSecurityRequirementMapping>> GetAllComponentSecurityRequirementMappingsAsync();
    }

    public interface IComponentThreatSecurityRequirementMappingRepository
    {
        Task<IEnumerable<ComponentThreatSecurityRequirementMapping>> GetAllComponentThreatSecurityRequirementMappingsAsync();
    }
}

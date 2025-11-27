using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;
using ThreatModeler.TF.Infra.Contract.Repository.Global;

namespace ThreatModeler.TF.Infra.Contract.Repository
{
    public interface IRepositoryHub
    {
        ISqlConnectionFactory ConnectionFactory { get; }
        ILibraryCacheService LibraryCache { get; }
        ILibraryRepository Libraries { get; }
        IThreatRepository Threats { get; }
        IComponentRepository Components { get; }
        ISecurityRequirementRepository SecurityRequirements { get; }
        ITestcaseRepository Testcases { get; }
        IPropertyRepository Properties { get; }
        IPropertyOptionRepository PropertyOptions { get; }
        IComponentSecurityRequirementMappingRepository ComponentSecurityRequirementMappings { get; }
        IComponentThreatMappingRepository ComponentThreatMappings { get; }
        IComponentThreatSecurityRequirementMappingRepository ComponentThreatSecurityRequirementMappings { get; }
        IThreatSecurityRequirementMappingRepository ThreatSecurityRequirementMappings { get; }
        IComponentPropertyMappingRepository ComponentPropertyMappings { get; }
        IComponentPropertyOptionMappingRepository ComponentPropertyOptionMappings { get; }
        IComponentPropertyOptionThreatMappingRepository ComponentPropertyOptionThreatMappings { get; }
        IComponentPropertyOptionThreatSecurityRequirementMappingRepository ComponentPropertyOptionThreatSecurityRequirementMappings { get; }
        IComponentTypeRepository ComponentTypes { get; }
        IPropertyTypeRepository PropertyTypes { get; }
    }
}

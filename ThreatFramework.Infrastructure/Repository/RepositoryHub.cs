using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Repository;

namespace ThreatFramework.Infrastructure.Repository
{
    public sealed class RepositoryHub : IRepositoryHub
    {
        public RepositoryHub(ISqlConnectionFactory factory,
                             ILibraryRepository libraries,
                             ILibraryCacheService libraryCache,
                             IThreatRepository threats,
                             IComponentRepository components,
                             ISecurityRequirementRepository securityRequirements,
                             ITestcaseRepository testcases,
                             IPropertyRepository properties,
                             IPropertyOptionRepository propertyOptions,
                             IComponentSecurityRequirementMappingRepository csr,
                             IComponentThreatMappingRepository ct,
                             IComponentThreatSecurityRequirementMappingRepository ctsr,
                             IThreatSecurityRequirementMappingRepository tsr,
                             IComponentPropertyMappingRepository cp,
                             IComponentPropertyOptionMappingRepository cpo,
                             IComponentPropertyOptionThreatMappingRepository cpoth,
                             IComponentPropertyOptionThreatSecurityRequirementMappingRepository cpotsr)
        {
            ConnectionFactory = factory;
            LibraryCache = libraryCache;

            Libraries = libraries;
            Threats = threats;
            Components = components;
            SecurityRequirements = securityRequirements;
            Testcases = testcases;
            Properties = properties;
            PropertyOptions = propertyOptions;

            ComponentSecurityRequirementMappings = csr;
            ComponentThreatMappings = ct;
            ComponentThreatSecurityRequirementMappings = ctsr;
            ThreatSecurityRequirementMappings = tsr;
            ComponentPropertyMappings = cp;
            ComponentPropertyOptionMappings = cpo;
            ComponentPropertyOptionThreatMappings = cpoth;
            ComponentPropertyOptionThreatSecurityRequirementMappings = cpotsr;
        }

        public ISqlConnectionFactory ConnectionFactory { get; }
        public ILibraryCacheService LibraryCache { get; }

        public ILibraryRepository Libraries { get; }
        public IThreatRepository Threats { get; }
        public IComponentRepository Components { get; }
        public ISecurityRequirementRepository SecurityRequirements { get; }
        public ITestcaseRepository Testcases { get; }
        public IPropertyRepository Properties { get; }
        public IPropertyOptionRepository PropertyOptions { get; }

        public IComponentSecurityRequirementMappingRepository ComponentSecurityRequirementMappings { get; }
        public IComponentThreatMappingRepository ComponentThreatMappings { get; }
        public IComponentThreatSecurityRequirementMappingRepository ComponentThreatSecurityRequirementMappings { get; }
        public IThreatSecurityRequirementMappingRepository ThreatSecurityRequirementMappings { get; }
        public IComponentPropertyMappingRepository ComponentPropertyMappings { get; }
        public IComponentPropertyOptionMappingRepository ComponentPropertyOptionMappings { get; }
        public IComponentPropertyOptionThreatMappingRepository ComponentPropertyOptionThreatMappings { get; }
        public IComponentPropertyOptionThreatSecurityRequirementMappingRepository ComponentPropertyOptionThreatSecurityRequirementMappings { get; }
    }
}
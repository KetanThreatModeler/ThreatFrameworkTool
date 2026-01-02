using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.Infrastructure.Repository;
using ThreatFramework.Infrastructure.Services;
using ThreatModeler.TF.Infra.Contract.Repository;
using ThreatModeler.TF.Infra.Implmentation.Repository;
using ThreatModeler.TF.Infra.Implmentation.Repository.AssistRule;
using ThreatModeler.TF.Infra.Implmentation.Repository.CoreEntities;
using ThreatModeler.TF.Infra.Implmentation.Repository.Global;
using ThreatModeler.TF.Infra.Implmentation.Repository.ThreatMapping;

namespace ThreatFramework.Infrastructure
{
    public sealed class RepositoryHubFactory : IRepositoryHubFactory
    {
        private readonly DatabaseOptions _db;
        private readonly ILoggerFactory _loggerFactory;

        public RepositoryHubFactory(IOptions<DatabaseOptions> db, ILoggerFactory loggerFactory)
            => (_db, _loggerFactory) = (db.Value ?? throw new ArgumentNullException(nameof(db)), loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)));

        public IRepositoryHub Create(DataPlane plane)
        {
            var cs = plane == DataPlane.Trc ? _db.TrcConnectionString : _db.ClientConnectionString;

            // plane-scoped connection factory
            var factory = new SqlConnectionFactory(cs);

            // construct repos with the same factory
            var libraries = new LibraryRepository(factory, _loggerFactory.CreateLogger<LibraryRepository>());

            // plane-scoped cache service backed by the plane’s libraries repo
            var libraryCache = new LibraryCacheService(libraries);

            // now construct the rest with required deps
            var threats = new ThreatRepository(libraryCache, factory, _loggerFactory.CreateLogger<ThreatRepository>());
            var components = new ComponentRepository(libraryCache, factory, _loggerFactory.CreateLogger<ComponentRepository>());
            var componentTypes = new ComponentTypeRepository(libraryCache, factory, _loggerFactory.CreateLogger<ComponentTypeRepository>());
            var securityReqs = new SecurityRequirementRepository(libraryCache, factory, _loggerFactory.CreateLogger<SecurityRequirementRepository>());
            var testcases = new TestcaseRepository(libraryCache, factory);
            var properties = new PropertyRepository(libraryCache, factory);
            var propertyTypes = new PropertyTypeRepository(factory);
            var propertyOptions = new PropertyOptionRepository(factory, _loggerFactory.CreateLogger<PropertyOptionRepository>());

            var csr = new ComponentSecurityRequirementMappingRepository(factory, libraryCache, _loggerFactory.CreateLogger<ComponentSecurityRequirementMappingRepository>());
            var ct = new ComponentThreatMappingRepository(factory, libraryCache, _loggerFactory.CreateLogger<ComponentThreatMappingRepository>());
            var ctsr = new ComponentThreatSecurityRequirementMappingRepository(factory, libraryCache);
            var tsr = new ThreatSecurityRequirementMappingRepository(factory, libraryCache);
            var cp = new ComponentPropertyMappingRepository(factory, libraryCache);
            var cpo = new ComponentPropertyOptionMappingRepository(factory, libraryCache);
            var cpoth = new ComponentPropertyOptionThreatMappingRepository(factory, libraryCache);
            var cpotsr = new ComponentPropertyOptionThreatSecurityRequirementMappingRepository(factory, libraryCache);
            var relationships = new RelationshipRepository(factory, _loggerFactory.CreateLogger<RelationshipRepository>());
            var resourceTypeValues = new ResourceTypeValuesRepository(factory, libraryCache, _loggerFactory.CreateLogger<ResourceTypeValuesRepository>());
            var resourceTypeValueRelationships = new ResourceTypeValueRelationshipRepository(factory, libraryCache, _loggerFactory.CreateLogger<ResourceTypeValueRelationshipRepository>());
            return new RepositoryHub(factory, libraries, libraryCache, threats, components, componentTypes, securityReqs,
                                     testcases, properties, propertyTypes, propertyOptions, csr, ct, ctsr, tsr,
                                     cp, cpo, cpoth, cpotsr, relationships, resourceTypeValues, resourceTypeValueRelationships);
        }
    }
}

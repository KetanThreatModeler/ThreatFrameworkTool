using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.Infrastructure.Repository;
using ThreatFramework.Infrastructure.Services;
using ThreatModeler.TF.Infra.Contract.Repository;
using ThreatModeler.TF.Infra.Implmentation.Repository;
using ThreatModeler.TF.Infra.Implmentation.Repository.Global;

namespace ThreatFramework.Infrastructure
{
    public sealed class RepositoryHubFactory : IRepositoryHubFactory
    {
        private readonly DatabaseOptions _db;

        public RepositoryHubFactory(IOptions<DatabaseOptions> db)
            => _db = db.Value ?? throw new ArgumentNullException(nameof(db));

        public IRepositoryHub Create(DataPlane plane)
        {
            var cs = plane == DataPlane.Trc ? _db.TrcConnectionString : _db.ClientConnectionString;

            // plane-scoped connection factory
            var factory = new SqlConnectionFactory(cs);

            // construct repos with the same factory
            var libraries = new LibraryRepository(factory);

            // plane-scoped cache service backed by the plane’s libraries repo
            var libraryCache = new LibraryCacheService(libraries);

            // now construct the rest with required deps
            var threats = new ThreatRepository(libraryCache, factory);
            var components = new ComponentRepository(libraryCache, factory);
            var componentTypes = new ComponentTypeRepository(libraryCache, factory);
            var securityReqs = new SecurityRequirementRepository(libraryCache, factory);
            var testcases = new TestcaseRepository(libraryCache, factory);
            var properties = new PropertyRepository(libraryCache, factory);
            var propertyTypes = new PropertyTypeRepository(factory);
            var propertyOptions = new PropertyOptionRepository(factory);

            var csr = new ComponentSecurityRequirementMappingRepository(factory,libraryCache);
            var ct = new ComponentThreatMappingRepository(factory, libraryCache);
            var ctsr = new ComponentThreatSecurityRequirementMappingRepository(factory, libraryCache);
            var tsr = new ThreatSecurityRequirementMappingRepository(factory, libraryCache);
            var cp = new ComponentPropertyMappingRepository(factory, libraryCache);
            var cpo = new ComponentPropertyOptionMappingRepository(factory, libraryCache);
            var cpoth = new ComponentPropertyOptionThreatMappingRepository(factory, libraryCache);
            var cpotsr = new ComponentPropertyOptionThreatSecurityRequirementMappingRepository(factory, libraryCache);

            return new RepositoryHub(factory, libraries, libraryCache, threats, components, componentTypes, securityReqs,
                                     testcases, properties, propertyTypes, propertyOptions, csr, ct, ctsr, tsr,
                                     cp, cpo, cpoth, cpotsr);
        }
    }
}

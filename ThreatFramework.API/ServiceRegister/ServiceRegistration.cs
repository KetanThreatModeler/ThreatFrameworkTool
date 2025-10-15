using ThreatFramework.Core.Config;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.Infrastructure;
using ThreatFramework.Infrastructure.Index;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatFramework.YamlFileGenerator.Impl;

namespace ThreatFramework.API.ServiceRegister
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config)
        {
            // Export options (paths, LibraryIds per tenant)
            services.AddOptions<YamlExportOptions>()
                .Bind(config.GetSection("YamlExport"))
                .Validate(o => o.Trc is not null && o.Client is not null, "YamlExport requires Trc & Client")
                .ValidateOnStart();

            // DatabaseOptions for Infra
            services.AddOptions<DatabaseOptions>()
                .Configure(o =>
                {
                    o.TrcConnectionString = config.GetConnectionString("TrcDb")
                        ?? throw new InvalidOperationException("ConnectionStrings:TrcDb missing");
                    o.ClientConnectionString = config.GetConnectionString("ClientDb")
                        ?? throw new InvalidOperationException("ConnectionStrings:ClientDb missing");
                })
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Plane hub factory (single registration)
            services.AddSingleton<IRepositoryHubFactory, RepositoryHubFactory>();

            // Cross-cutting services for generator (e.g., GuidIndexService, your YAML templates)
            services.AddScoped<IGuidIndexService, GuidIndexService>();

            // Orchestrators (client/TRC)
            services.AddScoped<IYamlFileGeneratorForClient, ClientYamlFilesGenerator>();
            services.AddScoped<IYamlFilesGeneratorForTRC, TrcYamlFilesGenerator>();

            return services;
        }
    }
}

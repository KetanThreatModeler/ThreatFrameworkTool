using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.Infrastructure;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatFramework.YamlFileGenerator.Impl;
using ThreatModeler.TF.Infra.Contract.Index.TRC;
using ThreatModeler.TF.Infra.Implmentation.Index.TRC;
using ThreatModeler.TF.YamlFileGenerator.Implementation;

namespace ThreatFramework.API.ServiceRegister
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config)
        {
           
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
            services.AddScoped<ITRCGuidIndexService, TRCGuidIndexService>();

            // Orchestrators (client/TRC)
            services.AddScoped<IYamlFileGeneratorForClient, ClientYamlFilesGenerator>();
            services.AddScoped<IYamlFilesGeneratorForTRC, TrcYamlFilesGenerator>();

            return services;
        }
    }
}

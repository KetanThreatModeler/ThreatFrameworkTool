using ThreatFramework.API.ServiceRegister;
using ThreatFramework.Drift.Contract;
using ThreatFramework.Drift.Contract.CoreEntityDriftService;
using ThreatFramework.Drift.Contract.FolderDiff;
using ThreatFramework.Drift.Contract.MappingDriftService;
using ThreatFramework.Drift.Contract.MappingDriftService.Builder;
using ThreatFramework.Drift.Impl;
using ThreatFramework.Drift.Impl.CoreEntityDriftService;
using ThreatFramework.Drift.Impl.FolderDiff;
using ThreatFramework.Drift.Impl.MappingDriftService;
using ThreatFramework.Drift.Impl.MappingDriftService.Builder;
using ThreatFramework.Git.Contract;
using ThreatFramework.Git.Impl;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.DataInsertion;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.Infra.Contract.YamlRepository;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using ThreatFramework.Infrastructure;
using ThreatFramework.Infrastructure.DataInsertion;
using ThreatFramework.Infrastructure.Index;
using ThreatFramework.Infrastructure.Repository;
using ThreatFramework.Infrastructure.Services;
using ThreatFramework.Infrastructure.YamlRepository;
using ThreatFramework.Infrastructure.YamlRepository.CoreEntities;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatFramework.YamlFileGenerator.Impl;
using ThreatModeler.TF.Core.CoreEntities;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Drift.Implemenetation;
using ThreatModeler.TF.Git.Contract;
using ThreatModeler.TF.Git.Contract.PathProcessor;
using ThreatModeler.TF.Git.Implementation;
using ThreatModeler.TF.Git.Implementation.PathProcessor;
using ThreatModeler.TF.Infra.Contract.Repository.Global;
using ThreatModeler.TF.Infra.Contract.YamlRepository.Global;
using ThreatModeler.TF.Infra.Implmentation.Repository;
using ThreatModeler.TF.Infra.Implmentation.Repository.Global;
using ThreatModeler.TF.Infra.Implmentation.YamlRepository.Global;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddOptions<PathOptions>()
    .Bind(builder.Configuration.GetSection(PathOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Core services
builder.Services.AddScoped<ILibraryCacheService, LibraryCacheService>();

builder.Services.AddScoped<ISqlConnectionFactory>(sp =>
    new SqlConnectionFactory(builder.Configuration.GetConnectionString("ClientDb")));
// Repository registrations
builder.Services.AddScoped<ILibraryRepository, LibraryRepository>();
builder.Services.AddScoped<IComponentRepository, ComponentRepository>();
builder.Services.AddScoped<IComponentTypeRepository, ComponentTypeRepository>();
builder.Services.AddScoped<IThreatRepository, ThreatRepository>();
builder.Services.AddScoped<ISecurityRequirementRepository, SecurityRequirementRepository>();
builder.Services.AddScoped<ITestcaseRepository, TestcaseRepository>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IPropertyTypeRepository, PropertyTypeRepository>();
builder.Services.AddScoped<IPropertyOptionRepository, PropertyOptionRepository>();

// Mapping repository registrations
builder.Services.AddScoped<IComponentPropertyMappingRepository, ComponentPropertyMappingRepository>();
builder.Services.AddScoped<IComponentPropertyOptionMappingRepository, ComponentPropertyOptionMappingRepository>();
builder.Services.AddScoped<IComponentPropertyOptionThreatMappingRepository, ComponentPropertyOptionThreatMappingRepository>();
builder.Services.AddScoped<IComponentPropertyOptionThreatSecurityRequirementMappingRepository, ComponentPropertyOptionThreatSecurityRequirementMappingRepository>();
builder.Services.AddScoped<IThreatSecurityRequirementMappingRepository, ThreatSecurityRequirementMappingRepository>();
builder.Services.AddScoped<IComponentThreatMappingRepository, ComponentThreatMappingRepository>();
builder.Services.AddScoped<IComponentThreatSecurityRequirementMappingRepository, ComponentThreatSecurityRequirementMappingRepository>();
builder.Services.AddScoped<IComponentSecurityRequirementMappingRepository, ComponentSecurityRequirementMappingRepository>();

builder.Services.AddScoped<IYamlComponentReader, YamlComponentReader>();
builder.Services.AddScoped<IYamlLibraryReader, YamlLibraryReader>();
builder.Services.AddScoped<IYamlThreatReader, YamlThreatReader>();
builder.Services.AddScoped<IYamlSecurityRequirementReader, YamlSecurityRequirementReader>();
builder.Services.AddScoped<IYamlTestcaseReader, YamlTestcaseReader>();
builder.Services.AddScoped<IYamlPropertyReader, YamlPropertyReader>();
builder.Services.AddScoped<IYamlPropertyOptionReader, YamlPropertyOptionReader>();

builder.Services.AddScoped<IYamlComponentThreatReader, YamlComponentThreatReader>();
builder.Services.AddScoped<IYamlComponentThreatSRReader, YamlComponentThreatSRsReader>();
builder.Services.AddScoped<IYamlComponentSRReader, YamlComponentSRReaders>();
builder.Services.AddScoped<IYamlThreatSrReader, YamlThreatSrReader>();
builder.Services.AddScoped<IYamlComponentPropertyReader, YamlComponentPropertyReader>();
builder.Services.AddScoped<IYamlComponentPropertyOptionReader, YamlComponentPropertyOptionReader>();
builder.Services.AddScoped<IYamlComponentPropertyOptionThreatReader, YamlCpoThreatReader>();
builder.Services.AddScoped<IYamlCpoThreatSrReader, YamlCpoThreatSrReader>();

builder.Services.AddTransient<IGitService, GitService>();
builder.Services.AddScoped<IDiffSummaryService, RemoteVsFolderDiffService>();
builder.Services.AddScoped<IFolderToFolderDiffService, FolderToFolderDiffService>();

builder.Services.AddSingleton<IFileSystem, LocalFileSystem>();
builder.Services.AddSingleton<IYamlSerializer, YamlDotNetSerializer>();
builder.Services.AddSingleton<IFolderMap, DefaultFolderMap>();
builder.Services.AddSingleton<IIdentityResolver, ReflectionIdentityResolver>();

// Generic repository & engines
builder.Services.AddSingleton(typeof(IEntityRepository<>), typeof(FileSystemEntityRepository<>));

// YAML generator service
builder.Services.AddScoped<IYamlFileGenerator, YamlFilesGenerator>();

builder.Services.AddScoped<IYamlReaderRouter, YamlReaderRouter>();
builder.Services.AddScoped<ICoreEntityDrift, CoreEntityDrift>();

builder.Services.AddScoped<IGuidSource, GuidSource>();
builder.Services.AddSingleton<IGuidIndexRepository, GuidIndexRepository>(); // <-- interface
builder.Services.AddScoped<IGuidIndexService, GuidIndexService>();
builder.Services.AddMemoryCache();
builder.Services.AddAppServices(builder.Configuration);
builder.Services.AddSingleton<IFolderDiffService, FolderDiffService>();
builder.Services.AddScoped<ILibraryDriftAggregator, LibraryDriftAggregator>();
builder.Services.AddScoped<IComponentPropertyMappingDriftService, ComponentPropertyMappingDriftService>();
builder.Services.AddScoped<IComponentMappingDriftAggregator, ComponentMappingDriftAggregator>();
builder.Services.AddScoped<IComponentThreatSRDriftService, ComponentThreatSRDriftService>();
builder.Services.AddScoped<IComponentSRDriftService, ComponentSRDriftService>();
builder.Services.AddScoped<IComponentMappingDriftService, ComponentMappingDriftService>();
builder.Services.AddScoped<IComponentPropertyGraphBuilder, ComponentPropertyGraphBuilder>();
builder.Services.AddScoped<IComponentSRGraphBuilder, ComponentSRGraphBuilder>();
builder.Services.AddScoped<IComponentThreatSRGraphBuilder, ComponentThreatSRGraphBuilder>();
builder.Services.AddScoped<IGuidLookupRepository, SqlServerGuidLookupRepository>();
builder.Services.AddScoped<IGuidIntegrityService, GuidIntegrityService>();
builder.Services.AddScoped<IDriftApplier, DriftApplier>();


builder.Services.AddScoped<IGitFolderDiffService, GitFolderDiffService>();


builder.Services.AddScoped<IPathClassifier, DefaultPathClassifier>();
builder.Services.AddScoped<IRepositoryDiffEntityPathService, RepositoryDiffEntityPathService>();
builder.Services.AddScoped<ILibraryScopedDiffService, LibraryScopedDiffService>();
builder.Services.AddScoped<IFinalDriftService, FinalDriftService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

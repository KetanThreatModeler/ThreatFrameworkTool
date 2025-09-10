using Microsoft.Extensions.Options;
using ThreatFramework.Core.Git;
using ThreatFramework.Git.Contract;
using ThreatFramework.Git.Impl;
using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.Infrastructure;
using ThreatFramework.Infrastructure.Configuration;
using ThreatFramework.Infrastructure.Repository;
using ThreatFramework.Infrastructure.Services;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatFramework.YamlFileGenerator.Impl;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure ThreatModeling options
builder.Services.Configure<ThreatModelingOptions>(
    builder.Configuration.GetSection(ThreatModelingOptions.SectionName));

// Core services
builder.Services.AddSingleton<IIndexService, IndexService>();
builder.Services.AddScoped<ILibraryCacheService, LibraryCacheService>();
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>(); 

// Repository registrations
builder.Services.AddScoped<ILibraryRepository, LibraryRepository>();
builder.Services.AddScoped<IComponentRepository, ComponentRepository>();
builder.Services.AddScoped<IThreatRepository, ThreatRepository>();
builder.Services.AddScoped<ISecurityRequirementRepository, SecurityRequirementRepository>();
builder.Services.AddScoped<ITestcaseRepository, TestcaseRepository>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
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

builder.Services.AddScoped<IDiffSummaryService, RemoteVsFolderDiffService>();
builder.Services.AddScoped<IFolderToFolderDiffService, FolderToFolderDiffService>();

// YAML generator service
builder.Services.AddScoped<IYamlFileGenerator, YamlFilesGenerator>();

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

using ThreatFramework.Infra.Contract;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.Infrastructure;
using ThreatFramework.Infrastructure.Configuration;
using ThreatFramework.Infrastructure.Repository;
using ThreatFramework.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure ThreatModeling options
builder.Services.Configure<ThreatModelingOptions>(
    builder.Configuration.GetSection(ThreatModelingOptions.SectionName));

builder.Services.AddSingleton<IIndexService, IndexService>();
builder.Services.AddSingleton<ILibraryCacheService, LibraryCacheService>();
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>(); 
builder.Services.AddScoped<ILibraryRepository, LibraryRepository>();
builder.Services.AddScoped<IComponentRepository, ComponentRepository>();
builder.Services.AddScoped<IThreatRepository, ThreatRepository>();
builder.Services.AddScoped<ISecurityRequirementRepository, SecurityRequirementRepository>();
builder.Services.AddScoped<ITestcaseRepository, TestcaseRepository>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IPropertyOptionRepository, PropertyOptionRepository>();
builder.Services.AddScoped<IComponentPropertyMappingRepository, ComponentPropertyMappingRepository>();
builder.Services.AddScoped<IComponentPropertyOptionMappingRepository, ComponentPropertyOptionMappingRepository>();
builder.Services.AddScoped<IComponentPropertyOptionThreatMappingRepository, ComponentPropertyOptionThreatMappingRepository>();
builder.Services.AddScoped<IComponentPropertyOptionThreatSecurityRequirementMappingRepository, ComponentPropertyOptionThreatSecurityRequirementMappingRepository>();
builder.Services.AddScoped<IThreatSecurityRequirementMappingRepository, ThreatSecurityRequirementMappingRepository>();
builder.Services.AddScoped<IComponentThreatMappingRepository, ComponentThreatMappingRepository>();
builder.Services.AddScoped<IComponentThreatSecurityRequirementMappingRepository, ComponentThreatSecurityRequirementMappingRepository>();

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

using ThreatFramework.Infrastructure.Configuration;
using ThreatFramework.Infrastructure.Interfaces;
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

// Register Index Service as singleton for better performance
builder.Services.AddSingleton<IIndexService, IndexService>();

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

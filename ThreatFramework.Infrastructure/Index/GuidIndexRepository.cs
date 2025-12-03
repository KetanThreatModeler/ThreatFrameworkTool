using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract.Index;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ThreatFramework.Infrastructure.Index
{
    public sealed class GuidIndexRepository : IGuidIndexRepository
    {
        private readonly ILogger<GuidIndexRepository> _logger;

        private static readonly IDeserializer _deserializer =
            new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

        public GuidIndexRepository(ILogger<GuidIndexRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<GuidIndex>> LoadAsync(string path, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.LogError("GuidIndexRepository: No path supplied for index loading.");
                throw new ArgumentException("Index file path must be provided.", nameof(path));
            }

            if (!File.Exists(path))
            {
                _logger.LogError("GuidIndexRepository: Index file not found at path: {Path}", path);
                throw new FileNotFoundException($"Index file not found at '{path}'.", path);
            }

            _logger.LogInformation("Loading GUID index YAML from: {Path}", path);

            string yaml;
            try
            {
                yaml = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
                _logger.LogDebug("Successfully read YAML file: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read YAML file at path: {Path}", path);
                throw;
            }

            GuidIndexYaml? wrapper;
            try
            {
                wrapper = _deserializer.Deserialize<GuidIndexYaml>(yaml);
                _logger.LogDebug("YAML deserialized successfully for {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize YAML index file: {Path}", path);
                throw new InvalidDataException($"Invalid YAML format in index file '{path}'.", ex);
            }

            if (wrapper?.Entities == null || wrapper.Entities.Count == 0)
            {
                _logger.LogWarning("YAML 'entities' section missing or empty in file: {Path}", path);
                return Array.Empty<GuidIndex>();
            }

            _logger.LogInformation("Loaded {Count} GuidIndex records from file: {Path}", wrapper.Entities.Count, path);
            LogEntityTypeSummary(wrapper.Entities);

            return wrapper.Entities;
        }

        private void LogEntityTypeSummary(IEnumerable<GuidIndex> records)
        {
            var grouped = records
                .GroupBy(r => r.EntityType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToList();

            foreach (var item in grouped)
            {
                _logger.LogInformation("Loaded {Count} entities of type {EntityType}", item.Count, item.Type);
            }
        }

        /// <summary>
        /// Wrapper that matches the YAML produced by GuidIndexService:
        /// var yamlData = new { Entities = entityIndexDto };
        /// </summary>
        private sealed class GuidIndexYaml
        {
            public List<GuidIndex> Entities { get; set; } = new();
        }
    }
}

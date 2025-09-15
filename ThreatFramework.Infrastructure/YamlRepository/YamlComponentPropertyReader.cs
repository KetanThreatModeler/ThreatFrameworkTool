using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Infra.Contract.YamlRepository;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public class YamlComponentPropertyReader : YamlReaderBase, IYamlComponentPropertyReader
    {
        private readonly ILogger<YamlComponentPropertyReader> _logger;

        public YamlComponentPropertyReader(ILogger<YamlComponentPropertyReader> logger) => _logger = logger;

        public async Task<IReadOnlyList<ComponentPropertyMapping>> GetAllAsync(string folderPath, CancellationToken ct = default)
        {
            if (!Directory.Exists(folderPath))
            {
                _logger.LogError("YAML folder not found: {Folder}", folderPath);
                throw new DirectoryNotFoundException(folderPath);
            }

            var results = new List<ComponentPropertyMapping>();

            foreach (var file in EnumerateYamlFiles(folderPath))
            {
                ct.ThrowIfCancellationRequested();

                string yaml;
                try
                {
                    yaml = await File.ReadAllTextAsync(file, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read YAML file: {File}", file);
                    continue;
                }

                try
                {
                    if (!TryLoadSpec(yaml, out var spec, out _))
                    {
                        _logger.LogWarning("Missing 'spec' node. Skipping: {File}", file);
                        continue;
                    }

                    var componentGuidStr = RequiredScalar(spec, "componentGuid", file);
                    var propertyGuidStr = RequiredScalar(spec, "propertyGuid", file);

                    results.Add(new ComponentPropertyMapping
                    {
                        Id = 0,
                        ComponentGuid = G(componentGuidStr, "componentGuid", file),
                        PropertyGuid = G(propertyGuidStr, "propertyGuid", file),
                        IsOptional = GetBool(spec, "isOptional", false),
                        IsHidden = GetBool(spec, "isHidden", false),
                        IsOverridden = GetBool(spec, "isOverridden", false)
                    });
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse 'spec' for file: {File}", file);
                }
            }

            return results;
        }
    }
}

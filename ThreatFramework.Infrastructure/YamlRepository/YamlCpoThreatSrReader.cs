using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Infra.Contract.YamlRepository;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public sealed class YamlCpoThreatSrReader : YamlReaderBase, IYamlCpoThreatSrReader
    {
        private readonly ILogger<YamlCpoThreatSrReader> _logger;

        public YamlCpoThreatSrReader(ILogger<YamlCpoThreatSrReader> logger) => _logger = logger;

        public async Task<List<ComponentPropertyOptionThreatSecurityRequirementMapping>> GetAllAsync(
            string folderPath, CancellationToken ct = default)
        {
            folderPath = Path.Combine(folderPath, "mappings", "component-property-option-threat-security-requirement");

            if (!Directory.Exists(folderPath))
            {
                _logger.LogError("YAML folder not found: {MappingFolder}", folderPath);
                throw new DirectoryNotFoundException($"Folder {folderPath} does not exist");
            }

            var results = new List<ComponentPropertyOptionThreatSecurityRequirementMapping>();

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
                    var stream = new YamlStream();
                    using var sr = new StringReader(yaml);
                    stream.Load(sr);

                    if (stream.Documents.Count == 0 ||
                        stream.Documents[0].RootNode is not YamlMappingNode root)
                    {
                        _logger.LogWarning("Malformed YAML (no document/root mapping). Skipping: {File}", file);
                        continue;
                    }

                    // SPEC ONLY
                    if (!TryGetMap(root, "spec", out var spec))
                    {
                        _logger.LogWarning("Missing 'spec' node. Skipping: {File}", file);
                        continue;
                    }

                    var componentGuidStr = RequiredScalar(spec, "componentGuid", file);
                    var propertyGuidStr = RequiredScalar(spec, "propertyGuid", file);
                    var propertyOptionGuidStr = RequiredScalar(spec, "propertyOptionGuid", file);
                    var threatGuidStr = RequiredScalar(spec, "threatGuid", file);
                    var securityRequirementGuidStr = RequiredScalar(spec, "securityRequirementGuid", file);

                    results.Add(new ComponentPropertyOptionThreatSecurityRequirementMapping
                    {
                        Id = 0, // spec-only: no metadata.id
                        ComponentGuid = G(componentGuidStr, "componentGuid", file),
                        PropertyGuid = G(propertyGuidStr, "propertyGuid", file),
                        PropertyOptionGuid = G(propertyOptionGuidStr, "propertyOptionGuid", file),
                        ThreatGuid = G(threatGuidStr, "threatGuid", file),
                        SecurityRequirementGuid = G(securityRequirementGuidStr, "securityRequirementGuid", file),
                        IsHidden = false,      // spec-only: flags ignored
                        IsOverridden = false   // spec-only: flags ignored
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

        // ----- tiny helpers (spec-only) -----
        private static bool TryGetMap(YamlMappingNode map, string key, out YamlMappingNode child)
        {
            var k = new YamlScalarNode(key);
            if (map.Children.TryGetValue(k, out var node) && node is YamlMappingNode m)
            {
                child = m;
                return true;
            }
            child = default!;
            return false;
        }

        private static bool TryGetScalar(YamlMappingNode map, string key, out string value)
        {
            value = "";
            var k = new YamlScalarNode(key);
            if (map.Children.TryGetValue(k, out var node) && node is YamlScalarNode s && s.Value is not null)
            {
                value = s.Value;
                return true;
            }
            return false;
        }

        private static string RequiredScalar(YamlMappingNode map, string key, string file)
        {
            if (TryGetScalar(map, key, out var v) && !string.IsNullOrWhiteSpace(v)) return v;
            throw new InvalidOperationException($"Missing required field '{key}' in '{file}'.");
        }
    }
}


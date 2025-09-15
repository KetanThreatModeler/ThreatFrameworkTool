using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Infra.Contract.YamlRepository;

namespace ThreatFramework.Infrastructure.YamlRepository
{
        public sealed class YamlCpoThreatReader : YamlReaderBase, IYamlComponentPropertyOptionThreatReader
        {
            private readonly ILogger<YamlCpoThreatReader> _logger;
            public YamlCpoThreatReader(ILogger<YamlCpoThreatReader> logger) => _logger = logger;

            public async Task<IReadOnlyList<ComponentPropertyOptionThreatMapping>> GetAllAsync(
                string folderPath, CancellationToken ct = default)
            {
                if (!Directory.Exists(folderPath))
                {
                    _logger.LogError("YAML folder not found: {Folder}", folderPath);
                    throw new DirectoryNotFoundException(folderPath);
                }

                var results = new List<ComponentPropertyOptionThreatMapping>();

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
                        // Reusable spec loader
                        if (!TryLoadSpec(yaml, out var spec, out _))
                        {
                            _logger.LogWarning("Missing 'spec' node. Skipping: {File}", file);
                            continue;
                        }

                        var componentGuidStr = RequiredScalar(spec, "componentGuid", file);
                        var propertyGuidStr = RequiredScalar(spec, "propertyGuid", file);
                        var propertyOptionGuidStr = RequiredScalar(spec, "propertyOptionGuid", file);
                        var threatGuidStr = RequiredScalar(spec, "threatGuid", file);

                        results.Add(new ComponentPropertyOptionThreatMapping
                        {
                            Id = 0, // spec-only
                            ComponentGuid = G(componentGuidStr, "componentGuid", file),
                            PropertyGuid = G(propertyGuidStr, "propertyGuid", file),
                            PropertyOptionGuid = G(propertyOptionGuidStr, "propertyOptionGuid", file),
                            ThreatGuid = G(threatGuidStr, "threatGuid", file),
                            IsHidden = false,
                            IsOverridden = false
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

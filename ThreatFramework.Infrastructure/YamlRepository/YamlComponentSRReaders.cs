using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.ComponentMapping;
using ThreatFramework.Infra.Contract.YamlRepository;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public class YamlComponentSRReaders : YamlReaderBase, IYamlComponentSRReader
    {
        private readonly ILogger<YamlComponentSRReaders> _logger;

        public YamlComponentSRReaders(ILogger<YamlComponentSRReaders> logger) => _logger = logger;

        public async Task<List<ComponentSecurityRequirementMapping>> GetAllComponentSRAsync(string folderPath, CancellationToken ct = default)
        {
            folderPath = Path.Combine(folderPath, "mappings", "component-security-requirement");

            if (!Directory.Exists(folderPath)) 
            {
                _logger.LogError("YAML folder not found: {MappingFolder}", folderPath);
                throw new DirectoryNotFoundException($"Folder {folderPath} does not exist");
            }
          
            var results = new List<ComponentSecurityRequirementMapping>();

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
                    var securityRequirementGuidStr = RequiredScalar(spec, "securityRequirementGuid", file);

                    // Extract flags if they exist, otherwise use defaults
                    bool isHidden = false;
                    bool isOverridden = false;
                    
                    if (TryGetMap(spec, "flags", out var flagsMap))
                    {
                        isHidden = GetBool(flagsMap, "isHidden", false);
                        isOverridden = GetBool(flagsMap, "isOverridden", false);
                    }
                    else
                    {
                        // Fallback: check if flags are directly in spec (backward compatibility)
                        isHidden = GetBool(spec, "isHidden", false);
                        isOverridden = GetBool(spec, "isOverridden", false);
                    }

                    results.Add(new ComponentSecurityRequirementMapping
                    {
                        ComponentGuid = G(componentGuidStr, "componentGuid", file),
                        SecurityRequirementGuid = G(securityRequirementGuidStr, "securityRequirementGuid", file),
                        IsHidden = isHidden,
                        IsOverridden = isOverridden
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

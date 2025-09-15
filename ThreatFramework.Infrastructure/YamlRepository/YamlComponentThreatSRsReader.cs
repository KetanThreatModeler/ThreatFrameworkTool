using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ThreatFramework.Core.ComponentMapping;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Infra.Contract.YamlRepository;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public class YamlComponentThreatSRsReader : YamlReaderBase,IYamlComponentThreatSRReader 
    {
        private readonly ILogger<YamlComponentThreatSRsReader> _logger;

        public YamlComponentThreatSRsReader(ILogger<YamlComponentThreatSRsReader> logger) => _logger = logger;

        public async Task<IReadOnlyList<ComponentThreatSecurityRequirementMapping>> GetAllAsync(string folderPath, CancellationToken ct = default)
        {
            if (!Directory.Exists(folderPath))
            {
                _logger.LogError("YAML folder not found: {Folder}", folderPath);
                throw new DirectoryNotFoundException(folderPath);
            }

            var results = new List<ComponentThreatSecurityRequirementMapping>();

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
                    var threatGuidStr = RequiredScalar(spec, "threatGuid", file);
                    var securityRequirementGuidStr = RequiredScalar(spec, "securityRequirementGuid", file);

                    results.Add(new ComponentThreatSecurityRequirementMapping
                    {
                        Id = 0,
                        ComponentGuid = G(componentGuidStr, "componentGuid", file),
                        ThreatGuid = G(threatGuidStr, "threatGuid", file),
                        SecurityRequirementGuid = G(securityRequirementGuidStr, "securityRequirementGuid", file),
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

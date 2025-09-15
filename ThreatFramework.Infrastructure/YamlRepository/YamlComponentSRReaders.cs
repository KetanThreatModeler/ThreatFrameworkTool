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

        public async Task<IReadOnlyList<ComponentSecurityRequirementMapping>> GetAllThreatSRAsync(string folderPath, CancellationToken ct = default)
        {
            if (!Directory.Exists(folderPath))
            {
                _logger.LogError("YAML folder not found: {Folder}", folderPath);
                throw new DirectoryNotFoundException(folderPath);
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

                    results.Add(new ComponentSecurityRequirementMapping
                    {
                        ComponentGuid = G(componentGuidStr, "componentGuid", file),
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

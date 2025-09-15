using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreatFramework.Core.ComponentMapping;
using ThreatFramework.Infra.Contract.YamlRepository;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public class YamlThreatSrReader : YamlReaderBase, IYamlThreatSrReader
    {
        private readonly ILogger<YamlThreatSrReader> _logger;

        public YamlThreatSrReader(ILogger<YamlThreatSrReader> logger) => _logger = logger;

        public async Task<IReadOnlyList<ThreatSecurityRequirementMapping>> GetAllAsync(string folderPath, CancellationToken ct = default)
        {
            if (!Directory.Exists(folderPath))
            {
                _logger.LogError("YAML folder not found: {Folder}", folderPath);
                throw new DirectoryNotFoundException(folderPath);
            }

            var results = new List<ThreatSecurityRequirementMapping>();

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

                    var threatGuidStr = RequiredScalar(spec, "threatGuid", file);
                    var securityRequirementGuidStr = RequiredScalar(spec, "securityRequirementGuid", file);

                    results.Add(new ThreatSecurityRequirementMapping
                    {
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
    }
}

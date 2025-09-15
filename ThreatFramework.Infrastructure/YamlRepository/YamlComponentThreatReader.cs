using Microsoft.Extensions.Logging;
using ThreatFramework.Core.ComponentMapping;
using ThreatFramework.Infra.Contract.YamlRepository;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public class YamlComponentThreatReader : YamlReaderBase, IYamlComponentThreatReader
    {
        private readonly ILogger<YamlComponentThreatReader> _logger;

        public YamlComponentThreatReader(ILogger<YamlComponentThreatReader> logger) => _logger = logger;

        public async Task<IReadOnlyList<ComponentThreatMapping>> GetAllAsync(string folderPath, CancellationToken ct = default)
        {
            if (!Directory.Exists(folderPath))
            {
                _logger.LogError("YAML folder not found: {Folder}", folderPath);
                throw new DirectoryNotFoundException(folderPath);
            }

            var results = new List<ComponentThreatMapping>();

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

                    results.Add(new ComponentThreatMapping
                    {
                        Id = 0,
                        ComponentGuid = G(componentGuidStr, "componentGuid", file),
                        ThreatGuid = G(threatGuidStr, "threatGuid", file),
                        IsHidden = GetBool(spec, "isHidden", false),
                        IsOverridden = GetBool(spec, "isOverridden", false),
                        UsedForMitigation = GetBool(spec, "usedForMitigation", false)
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

using Microsoft.Extensions.Logging;
using ThreatFramework.Core.PropertyMapping;
using ThreatFramework.Infra.Contract.YamlRepository;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public class YamlComponentPropertyOptionReader : YamlReaderBase, IYamlComponentPropertyOptionReader
    {
        private readonly ILogger<YamlComponentPropertyOptionReader> _logger;

        public YamlComponentPropertyOptionReader(ILogger<YamlComponentPropertyOptionReader> logger) => _logger = logger;

        public async Task<List<ComponentPropertyOptionMapping>> GetAllAsync(string folderPath, CancellationToken ct = default)
        {
            folderPath = Path.Combine(folderPath, "mappings", "component-property-option");

            if (!Directory.Exists(folderPath))
            {
                _logger.LogError("YAML folder not found: {MappingFolder}", folderPath);
                throw new DirectoryNotFoundException($"Folder {folderPath} does not exist");
            }
            

            var results = new List<ComponentPropertyOptionMapping>();

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
                    var propertyOptionGuidStr = RequiredScalar(spec, "propertyOptionGuid", file);

                    results.Add(new ComponentPropertyOptionMapping
                    {
                        Id = 0,
                        ComponentGuid = G(componentGuidStr, "componentGuid", file),
                        PropertyGuid = G(propertyGuidStr, "propertyGuid", file),
                        PropertyOptionGuid = G(propertyOptionGuidStr, "propertyOptionGuid", file),
                        IsDefault = GetBool(spec, "isDefault", false),
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

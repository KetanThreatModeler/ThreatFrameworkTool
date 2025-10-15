using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ThreatFramework.Core.Config;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository.CoreEntities
{
    public class YamlComponentReader : YamlReaderBase, IYamlComponentReader
    {
        private readonly ILogger<YamlComponentReader>? _logger;
        private readonly IGuidIndexService _guidIndexService;
        private readonly YamlExportOptions _exportOptions;

        public YamlComponentReader(IGuidIndexService guidIndexService, IOptions<YamlExportOptions> exportOptions, ILogger<YamlComponentReader>? logger = null)
        {
            _logger = logger;
            _guidIndexService = guidIndexService;
            _exportOptions = exportOptions.Value;
        }

        public async Task<Component> GetComponentByGuid(Guid guid)
        {
            var componentId = _guidIndexService.GetInt(guid);
            
            var filePath = Path.Combine(_exportOptions.Trc.OutputPath, "components", $"{componentId}.yaml");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Component file not found: {filePath}");
            }

            string yaml;
            try
            {
                yaml = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read component file: {filePath}", ex);
            }

            return ParseComponentFromYaml(yaml, filePath, componentId);
        }

        /// <summary>
        /// Parse a set of YAML files into Component entities.
        /// Only files with kind: component are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<Component>> GetComponentsFromFilesAsync(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null) throw new ArgumentNullException(nameof(yamlFilePaths));

            var components = new List<Component>();

            // We iterate sequentially (like the other reader) to keep logging + error flow simple and predictable.
            foreach (var file in yamlFilePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    if (!File.Exists(file))
                    {
                        _logger?.LogWarning("YAML file not found: {File}", file);
                        continue;
                    }

                    string yaml;
                    try
                    {
                        yaml = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                    }
                    catch (Exception readEx)
                    {
                        _logger?.LogWarning(readEx, "Failed to read YAML file: {File}", file);
                        continue;
                    }

                    // Validate kind
                    var kind = ReadKind(yaml);
                    if (!string.Equals(kind, "component", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogDebug("Skipping non-component YAML (kind: {Kind}) in {File}", kind ?? "<null>", file);
                        continue;
                    }

                    var component = ParseComponentFromYaml(yaml, file, 0); // ID typically set by persistence layer
                    components.Add(component);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse component from YAML file: {File}", file);
                }
            }

            return components;
        }

        private Component ParseComponentFromYaml(string yaml, string filePath, int componentId)
        {
            // Validate kind
            var kind = ReadKind(yaml);
            if (!string.Equals(kind, "component", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Invalid YAML kind '{kind}' in file: {filePath}");
            }

            // Root + spec
            if (!TryLoadSpec(yaml, out var spec, out var root))
            {
                throw new InvalidOperationException($"Missing 'spec' node in file: {filePath}");
            }

            // metadata
            if (!TryGetMap(root, "metadata", out var metadata))
            {
                throw new InvalidOperationException($"Missing 'metadata' node in file: {filePath}");
            }

            // Required metadata scalars
            var guidStr = RequiredScalar(metadata, "guid", filePath);
            var name = RequiredScalar(metadata, "name", filePath);
            var libraryGuidStr = RequiredScalar(metadata, "libraryId", filePath);

            // Optional metadata scalars
            TryGetScalar(metadata, "version", out var version);

            // metadata.labels: sequence -> comma-separated string (null if empty)
            string? labels = null;
            if (metadata.Children.TryGetValue(new YamlScalarNode("labels"), out var labelsNode)
                && labelsNode is YamlSequenceNode seq && seq.Children.Count > 0)
            {
                var labelValues = seq.Children
                    .OfType<YamlScalarNode>()
                    .Where(s => !string.IsNullOrWhiteSpace(s.Value))
                    .Select(s => s.Value!.Trim());
                var joined = string.Join(",", labelValues);
                labels = string.IsNullOrWhiteSpace(joined) ? null : joined;
            }

            // spec fields
            TryGetScalar(spec, "imagePath", out var imagePath);
            TryGetScalar(spec, "description", out var descriptionRaw);
            var description = string.IsNullOrWhiteSpace(descriptionRaw)
                ? null
                : System.Net.WebUtility.HtmlDecode(descriptionRaw);

            // spec.flags
            bool isHidden = false, isOverridden = false;
            if (TryGetMap(spec, "flags", out var flags))
            {
                isHidden = GetBool(flags, "isHidden", false);
                isOverridden = GetBool(flags, "isOverridden", false);
            }

            return new Component
            {
                Id = componentId,
                Guid = G(guidStr, "metadata.guid", filePath),
                LibraryGuid = G(libraryGuidStr, "metadata.libraryId", filePath),
                ComponentTypeId = 0,
                IsHidden = isHidden,
                IsOverridden = isOverridden,
                CreatedDate = File.GetCreationTimeUtc(filePath),
                LastUpdated = File.GetLastWriteTimeUtc(filePath),
                Name = name,
                ImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath,
                Labels = labels,
                Version = string.IsNullOrWhiteSpace(version) ? null : version,
                Description = description,
                ChineseDescription = null
            };
        }
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using ThreatModeler.TF.Core.Config;
using ThreatModeler.TF.Infra.Implmentation.Helper;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository.CoreEntities
{
    public class YamlComponentReader : YamlReaderBase, IYamlComponentReader
    {
        private readonly ILogger<YamlComponentReader>? _logger;
        private readonly IGuidIndexService _guidIndexService;
        private readonly PathOptions _pathOptions;

        public YamlComponentReader(IGuidIndexService guidIndexService, IOptions<PathOptions> pathOptions, ILogger<YamlComponentReader>? logger = null)
        {
            _logger = logger;
            _guidIndexService = guidIndexService;
            _pathOptions = pathOptions.Value;
        }

        public async Task<Component> GetComponentByGuid(Guid guid)
        {
            var componentId = _guidIndexService.GetInt(guid);

            var filePath = Path.Combine(_pathOptions.TrcOutput, "components", $"{componentId}.yaml");

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

            // Load root mapping (flat structure)
            var yamlStream = new YamlStream();
            using (var sr = new StringReader(yaml))
            {
                yamlStream.Load(sr);
            }

            if (yamlStream.Documents.Count == 0 || yamlStream.Documents[0].RootNode is not YamlMappingNode root)
            {
                throw new InvalidOperationException($"YAML root is not a mapping in file: {filePath}");
            }

            // Required top-level scalars
            var guidStr = RequiredScalar(root, "guid", filePath);
            var name = RequiredScalar(root, "name", filePath);
            var libraryGuidStr = RequiredScalar(root, "libraryGuid", filePath);
            var componentTypeGuid = RequiredScalar(root, "componentTypeGuid", filePath);

            // Optional scalars
            TryGetScalar(root, "version", out var version);
            TryGetScalar(root, "imagePath", out var imagePathRaw);
            TryGetScalar(root, "description", out var descriptionRaw);
            var description = string.IsNullOrWhiteSpace(descriptionRaw)
                ? null
                : System.Net.WebUtility.HtmlDecode(descriptionRaw);

            // ChineseDescription (note: casing from YAML)
            TryGetScalar(root, "ChineseDescription", out var chineseDescRaw);
            string? chineseDescription = string.IsNullOrWhiteSpace(chineseDescRaw)
                ? null
                : System.Net.WebUtility.HtmlDecode(chineseDescRaw);

            // labels: [] → comma-separated string → ToLabelList()
            string? labels = null;
            if (root.Children.TryGetValue(new YamlScalarNode("labels"), out var labelsNode)
                && labelsNode is YamlSequenceNode seq && seq.Children.Count > 0)
            {
                var labelValues = seq.Children
                    .OfType<YamlScalarNode>()
                    .Where(s => !string.IsNullOrWhiteSpace(s.Value))
                    .Select(s => s.Value!.Trim());

                var joined = string.Join(",", labelValues);
                labels = string.IsNullOrWhiteSpace(joined) ? null : joined;
            }

            // flags:
            //   isHidden, isOverridden
            bool isHidden = false, isOverridden = false;
            if (TryGetMap(root, "flags", out var flags))
            {
                isHidden = GetBool(flags, "isHidden", false);
                isOverridden = GetBool(flags, "isOverridden", false);
            }

            return new Component
            {
                Id = componentId,
                Guid = G(guidStr, "guid", filePath),
                LibraryGuid = G(libraryGuidStr, "libraryGuid", filePath),
                ComponentTypeGuid = G(componentTypeGuid, "componentTypeGuid", filePath),
                IsHidden = isHidden,
                IsOverridden = isOverridden,
                Name = name,
                ImagePath = string.IsNullOrWhiteSpace(imagePathRaw) ? null : imagePathRaw,
                Labels = labels.ToLabelList(),
                Version = string.IsNullOrWhiteSpace(version) ? null : version,
                Description = description,
                ChineseDescription = chineseDescription
            };
        }
    }
}

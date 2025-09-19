using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository.CoreEntities
{
    public class YamlComponentReader : YamlReaderBase, IYamlComponentReader
    {
        private readonly ILogger<YamlComponentReader>? _logger;

        public YamlComponentReader(ILogger<YamlComponentReader>? logger = null)
        {
            _logger = logger;
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

                    // Root + spec
                    if (!TryLoadSpec(yaml, out var spec, out var root))
                    {
                        _logger?.LogWarning("Missing 'spec' node. Skipping: {File}", file);
                        continue;
                    }

                    // metadata
                    if (!TryGetMap(root, "metadata", out var metadata))
                    {
                        _logger?.LogWarning("Missing 'metadata' node. Skipping: {File}", file);
                        continue;
                    }

                    // Required metadata scalars
                    var guidStr = RequiredScalar(metadata, "guid", file);
                    var name = RequiredScalar(metadata, "name", file);
                    var libraryGuidStr = RequiredScalar(metadata, "libraryId", file);

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

                    var component = new Component
                    {
                        Id = 0, // Typically set by persistence layer
                        Guid = G(guidStr, "metadata.guid", file),
                        LibraryGuid = G(libraryGuidStr, "metadata.libraryId", file),
                        ComponentTypeId = 0, // Not present in YAML; set elsewhere if needed
                        IsHidden = isHidden,
                        IsOverridden = isOverridden,
                        CreatedDate = File.GetCreationTimeUtc(file),
                        LastUpdated = File.GetLastWriteTimeUtc(file),
                        Name = name,
                        ImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath,
                        Labels = labels,
                        Version = string.IsNullOrWhiteSpace(version) ? null : version,
                        Description = description,
                        ChineseDescription = null // Not present in your YAML shape
                    };

                    components.Add(component);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse component from YAML file: {File}", file);
                }
            }

            return components;
        }
    }
}

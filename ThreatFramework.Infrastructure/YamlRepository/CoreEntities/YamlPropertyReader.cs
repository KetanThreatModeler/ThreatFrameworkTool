using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository.CoreEntities
{
    public class YamlPropertyReader : YamlReaderBase, IYamlPropertyReader
    {
        private readonly ILogger<YamlPropertyReader>? _logger;

        public YamlPropertyReader(ILogger<YamlPropertyReader>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Parse a set of YAML files into Property entities.
        /// Only files with kind: property are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<Property>> GetPropertiesFromFilesAsync(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null) throw new ArgumentNullException(nameof(yamlFilePaths));

            var properties = new List<Property>();

            // We iterate sequentially to keep logging + error flow simple and predictable.
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
                    if (!string.Equals(kind, "property", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogDebug("Skipping non-property YAML (kind: {Kind}) in {File}", kind ?? "<null>", file);
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
                    var libraryGuidStr = RequiredScalar(metadata, "libraryGuid", file);

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
                    var propertyTypeGuidStr = RequiredScalar(spec, "propertyTypeGuid", file);
                    TryGetScalar(spec, "description", out var descriptionRaw);
                    var description = string.IsNullOrWhiteSpace(descriptionRaw)
                        ? null
                        : System.Net.WebUtility.HtmlDecode(descriptionRaw);

                    // spec.flags
                    bool isSelected = false;
                    bool isOptional = false;
                    bool isGlobal = false;
                    bool isHidden = false;
                    bool isOverridden = false;

                    if (TryGetMap(spec, "flags", out var flags))
                    {
                        isSelected = GetBool(flags, "isSelected", false);
                        isOptional = GetBool(flags, "isOptional", false);
                        isGlobal = GetBool(flags, "isGlobal", false);
                        isHidden = GetBool(flags, "isHidden", false);
                        isOverridden = GetBool(flags, "isOverridden", false);
                    }

                    // spec.i18n.zh for Chinese translations
                    string? chineseName = null;
                    string? chineseDescription = null;
                    if (TryGetMap(spec, "i18n", out var i18n) && TryGetMap(i18n, "zh", out var zh))
                    {
                        TryGetScalar(zh, "name", out chineseName);
                        TryGetScalar(zh, "description", out var chineseDescRaw);
                        chineseDescription = string.IsNullOrWhiteSpace(chineseDescRaw)
                            ? null
                            : System.Net.WebUtility.HtmlDecode(chineseDescRaw);
                    }

                    // Parse dates (use file timestamps as fallback)
                    var createdDate = File.GetCreationTimeUtc(file);
                    var lastUpdated = File.GetLastWriteTimeUtc(file);

                    var property = new Property
                    {
                        Id = 0, // Typically set by persistence layer
                        Guid = G(guidStr, "metadata.guid", file),
                        LibraryGuid = G(libraryGuidStr, "metadata.libraryGuid", file),
                        PropertyTypeGuid = G(propertyTypeGuidStr, "spec.propertyTypeGuid", file),
                        IsSelected = isSelected,
                        IsOptional = isOptional,
                        IsGlobal = isGlobal,
                        IsHidden = isHidden,
                        IsOverridden = isOverridden,
                        CreatedDate = createdDate,
                        LastUpdated = lastUpdated,
                        Name = name,
                        ChineseName = string.IsNullOrWhiteSpace(chineseName) ? null : chineseName,
                        Labels = labels,
                        Description = description,
                        ChineseDescription = chineseDescription
                    };

                    properties.Add(property);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse property from YAML file: {File}", file);
                }
            }

            return properties;
        }
    }
}

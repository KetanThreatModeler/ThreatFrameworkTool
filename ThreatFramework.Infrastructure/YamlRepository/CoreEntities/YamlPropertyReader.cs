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

                    // Load root mapping (flat structure)
                    var yamlStream = new YamlStream();
                    using (var sr = new StringReader(yaml))
                    {
                        yamlStream.Load(sr);
                    }

                    if (yamlStream.Documents.Count == 0 || yamlStream.Documents[0].RootNode is not YamlMappingNode root)
                    {
                        _logger?.LogWarning("YAML root is not a mapping. Skipping: {File}", file);
                        continue;
                    }

                    // Required top-level scalars
                    var guidStr = RequiredScalar(root, "guid", file);
                    var name = RequiredScalar(root, "name", file);
                    var libraryGuidStr = RequiredScalar(root, "libraryGuid", file);
                    var propertyTypeGuidStr = RequiredScalar(root, "propertyTypeGuid", file);

                    // labels: [] => comma-separated string or null
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

                    // description
                    TryGetScalar(root, "description", out var descriptionRaw);
                    var description = string.IsNullOrWhiteSpace(descriptionRaw)
                        ? null
                        : System.Net.WebUtility.HtmlDecode(descriptionRaw);

                    // ChineseName / ChineseDescription (note the casing from your YAML)
                    TryGetScalar(root, "ChineseName", out var chineseNameRaw);
                    string? chineseName = string.IsNullOrWhiteSpace(chineseNameRaw) ? null : chineseNameRaw;

                    TryGetScalar(root, "ChineseDescription", out var chineseDescRaw);
                    string? chineseDescription = string.IsNullOrWhiteSpace(chineseDescRaw)
                        ? null
                        : System.Net.WebUtility.HtmlDecode(chineseDescRaw);

                    // flags:
                    //   isSelected, isOptional, isGlobal, isHidden, isOverridden
                    bool isSelected = false;
                    bool isOptional = false;
                    bool isGlobal = false;
                    bool isHidden = false;
                    bool isOverridden = false;

                    if (TryGetMap(root, "flags", out var flags))
                    {
                        isSelected = GetBool(flags, "isSelected", false);
                        isOptional = GetBool(flags, "isOptional", false);
                        isGlobal = GetBool(flags, "isGlobal", false);
                        isHidden = GetBool(flags, "isHidden", false);
                        isOverridden = GetBool(flags, "isOverridden", false);
                    }

                    // Use file timestamps
                    var createdDate = File.GetCreationTimeUtc(file);
                    var lastUpdated = File.GetLastWriteTimeUtc(file);

                    var property = new Property
                    {
                        Id = 0, // Typically set by persistence layer
                        Guid = G(guidStr, "guid", file),
                        LibraryGuid = G(libraryGuidStr, "libraryGuid", file),
                        PropertyTypeGuid = G(propertyTypeGuidStr, "propertyTypeGuid", file),
                        IsSelected = isSelected,
                        IsOptional = isOptional,
                        IsGlobal = isGlobal,
                        IsHidden = isHidden,
                        IsOverridden = isOverridden,
                        Name = name,
                        ChineseName = chineseName,
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

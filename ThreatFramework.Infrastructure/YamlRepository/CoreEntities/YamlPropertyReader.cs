using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Implmentation.Helper;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository.CoreEntities
{
    public class YamlPropertyReader : YamlReaderBase, IYamlPropertyReader
    {
        private readonly ILogger<YamlPropertyReader> _logger;

        private const string EntityDisplayName = "Property";

        public YamlPropertyReader(ILogger<YamlPropertyReader>? logger = null)
        {
            _logger = logger ?? NullLogger<YamlPropertyReader>.Instance;
        }

        /// <summary>
        /// Parse a set of YAML files into Property entities.
        /// Only files with kind: property are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<Property>> GetPropertiesFromFilesAsync(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null)
            {
                throw new ArgumentNullException(nameof(yamlFilePaths));
            }

            var properties = new List<Property>();

            // Sequential for predictable logging and error behavior
            foreach (var file in yamlFilePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    // DRY: shared IO helper
                    var yaml = await TryReadYamlFileAsync(file, _logger, cancellationToken: default);
                    if (yaml is null)
                    {
                        // Warning already logged by TryReadYamlFileAsync
                        continue;
                    }

                    var property = ParseProperty(yaml, file);
                    if (property is not null)
                    {
                        properties.Add(property);
                    }
                    // If null, ParseProperty already logged why it was skipped.
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to parse {Entity} from YAML file: {File}",
                        EntityDisplayName,
                        file);
                }
            }

            return properties;
        }

        public Task<Property> GetPropertyFromFileAsync(string yamlFilePath)
            => LoadYamlEntityAsync(
                yamlFilePath,
                _logger,
                ParseProperty,
                EntityDisplayName,
                cancellationToken: default);

        #region Parsing

        /// <summary>
        /// Parses a single Property from YAML content.
        /// Returns null if the document is not a 'property' kind or is malformed.
        /// For single-file API, LoadYamlEntityAsync will treat null as an error and throw.
        /// </summary>
        private Property? ParseProperty(string yaml, string filePath)
        {
            try
            {
                // Validate kind
                var kind = ReadKind(yaml);
                if (!string.Equals(kind, "property", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug(
                        "Skipping non-property YAML (kind: {Kind}) in {File}",
                        kind ?? "<null>",
                        filePath);

                    return null;
                }

                // Root is the spec for Property YAML
                if (!TryLoadRoot(yaml, out var root))
                {
                    _logger.LogWarning(
                        "YAML root is not a mapping. Skipping: {File}",
                        filePath);

                    return null;
                }

                // Required top-level scalars
                var guidStr = RequiredScalar(root, "guid", filePath);
                var name = RequiredScalar(root, "name", filePath);
                var libraryGuidStr = RequiredScalar(root, "libraryGuid", filePath);
                var propertyTypeGuidStr = RequiredScalar(root, "propertyTypeGuid", filePath);

                // labels: sequence => comma-separated string or null
                string? labels = null;
                if (root.Children.TryGetValue(new YamlScalarNode("labels"), out var labelsNode)
                    && labelsNode is YamlSequenceNode seq
                    && seq.Children.Count > 0)
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

                // ChineseName / ChineseDescription (note the casing from YAML)
                TryGetScalar(root, "ChineseName", out var chineseNameRaw);
                var chineseName = string.IsNullOrWhiteSpace(chineseNameRaw)
                    ? null
                    : chineseNameRaw;

                TryGetScalar(root, "ChineseDescription", out var chineseDescRaw);
                var chineseDescription = string.IsNullOrWhiteSpace(chineseDescRaw)
                    ? null
                    : System.Net.WebUtility.HtmlDecode(chineseDescRaw);

                // flags:
                //   isSelected, isOptional, isGlobal, isHidden, isOverridden
                var isSelected = false;
                var isOptional = false;
                var isGlobal = false;
                var isHidden = false;
                var isOverridden = false;

                if (TryGetMap(root, "flags", out var flags))
                {
                    isSelected = GetBool(flags, "isSelected", false);
                    isOptional = GetBool(flags, "isOptional", false);
                    isGlobal = GetBool(flags, "isGlobal", false);
                    isHidden = GetBool(flags, "isHidden", false);
                    isOverridden = GetBool(flags, "isOverridden", false);
                }

                var property = new Property
                {
                    Guid = G(guidStr, "guid", filePath),
                    LibraryGuid = G(libraryGuidStr, "libraryGuid", filePath),
                    PropertyTypeGuid = G(propertyTypeGuidStr, "propertyTypeGuid", filePath),
                    IsSelected = isSelected,
                    IsOptional = isOptional,
                    IsGlobal = isGlobal,
                    IsHidden = isHidden,
                    IsOverridden = isOverridden,
                    Name = name,
                    ChineseName = chineseName,
                    Labels = labels.ToLabelList(),
                    Description = description,
                    ChineseDescription = chineseDescription
                };

                return property;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to parse {Entity} YAML file: {File}",
                    EntityDisplayName,
                    filePath);

                return null;
            }
        }

        #endregion
    }
}

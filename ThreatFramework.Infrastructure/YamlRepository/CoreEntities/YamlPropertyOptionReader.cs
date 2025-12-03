using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using ThreatModeler.TF.Core.Global;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository.CoreEntities
{
    public class YamlPropertyOptionReader : YamlReaderBase, IYamlPropertyOptionReader
    {
        private readonly ILogger<YamlPropertyOptionReader>? _logger;

        public YamlPropertyOptionReader(ILogger<YamlPropertyOptionReader>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Parse a set of YAML files into PropertyOption entities.
        /// Only files with kind: property-option are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<PropertyOption>> GetPropertyOption(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null) throw new ArgumentNullException(nameof(yamlFilePaths));

            var propertyOptions = new List<PropertyOption>();

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
                    if (!string.Equals(kind, "property-option", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogDebug("Skipping non-property-option YAML (kind: {Kind}) in {File}", kind ?? "<null>", file);
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
                    var idStr = RequiredScalar(metadata, "id", file);
                    var propertyGuidStr = RequiredScalar(metadata, "propertyGuid", file);

                    // spec fields
                    var optionText = RequiredScalar(spec, "optionText", file);

                    // Optional Chinese option text
                    TryGetScalar(spec, "chineseOptionText", out var chineseOptionText);

                    // spec.flags
                    bool isDefault = false;
                    bool isHidden = false;
                    bool isOverridden = false;

                    if (TryGetMap(spec, "flags", out var flags))
                    {
                        isDefault = GetBool(flags, "isDefault", false);
                        isHidden = GetBool(flags, "isHidden", false);
                        isOverridden = GetBool(flags, "isOverridden", false);
                    }

                    // Parse ID and PropertyId
                    if (!int.TryParse(idStr, out var id))
                    {
                        _logger?.LogWarning("Invalid id value '{Id}' in {File}", idStr, file);
                        continue;
                    }

                    int? propertyId = null;
                    if (!string.IsNullOrWhiteSpace(propertyGuidStr) && int.TryParse(propertyGuidStr, out var parsedPropertyId))
                    {
                        propertyId = parsedPropertyId;
                    }

                    // Generate GUID from ID if not provided
                    var guid = Guid.NewGuid();

                    // Use file timestamps for dates
                    var createdDate = File.GetCreationTimeUtc(file);
                    var lastUpdated = File.GetLastWriteTimeUtc(file);

                    var propertyOption = new PropertyOption
                    {
                        Id = id,
                        PropertyId = propertyId,
                        IsDefault = isDefault,
                        IsHidden = isHidden,
                        IsOverridden = isOverridden,
                        Guid = guid,
                        OptionText = optionText,
                        ChineseOptionText = string.IsNullOrWhiteSpace(chineseOptionText) ? null : chineseOptionText
                    };

                    propertyOptions.Add(propertyOption);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse property option from YAML file: {File}", file);
                }
            }

            return propertyOptions;
        }
    }
}

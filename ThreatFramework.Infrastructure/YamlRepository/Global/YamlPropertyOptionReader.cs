using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ThreatFramework.Infrastructure.YamlRepository;
using ThreatModeler.TF.Core.Model.Global;
using ThreatModeler.TF.Infra.Contract.YamlRepository.Global;
using YamlDotNet.RepresentationModel;

namespace ThreatModeler.TF.Infra.Implmentation.YamlRepository.Global
{
    public class YamlPropertyOptionReader : YamlReaderBase, IYamlPropertyOptionReader
    {
        private readonly ILogger<YamlPropertyOptionReader> _logger;

        private const string EntityDisplayName = "PropertyOption";

        public YamlPropertyOptionReader(ILogger<YamlPropertyOptionReader>? logger = null)
        {
            _logger = logger ?? NullLogger<YamlPropertyOptionReader>.Instance;
        }

        /// <summary>
        /// Parse a set of YAML files into PropertyOption entities.
        /// Only files with kind: property-option are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<PropertyOption>> GetPropertyOption(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null)
            {
                throw new ArgumentNullException(nameof(yamlFilePaths));
            }

            var propertyOptions = new List<PropertyOption>();

            foreach (var file in yamlFilePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    // DRY: use shared IO helper from YamlReaderBase
                    var yaml = await TryReadYamlFileAsync(file, _logger, cancellationToken: default);
                    if (yaml is null)
                    {
                        // Warning already logged by TryReadYamlFileAsync
                        continue;
                    }

                    var propertyOption = ParsePropertyOption(yaml, file);
                    if (propertyOption is not null)
                    {
                        propertyOptions.Add(propertyOption);
                    }
                    // If null, ParsePropertyOption already logged why it was skipped.
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

            return propertyOptions;
        }

        public Task<PropertyOption> GetPropertyOptionFromFileAsync(string yamlFilePath)
            => LoadYamlEntityAsync(
                yamlFilePath,
                _logger,
                ParsePropertyOption,
                EntityDisplayName,
                cancellationToken: default);

        #region Parsing

        /// <summary>
        /// Parses a single PropertyOption from YAML content.
        /// Returns null if the document is not a 'property-option' kind or is malformed.
        /// For single-file API, LoadYamlEntityAsync will treat null as an error and throw.
        /// </summary>
        private PropertyOption? ParsePropertyOption(string yaml, string filePath)
        {
            try
            {
                // Validate kind
                var kind = ReadKind(yaml);
                if (!string.Equals(kind, "property-option", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug(
                        "Skipping non-property-option YAML (kind: {Kind}) in {File}",
                        kind ?? "<null>",
                        filePath);

                    return null;
                }

                // Root is the spec for PropertyOption YAML
                if (!TryLoadRoot(yaml, out var root))
                {
                    _logger.LogWarning(
                        "YAML root is not a mapping. Skipping: {File}",
                        filePath);

                    return null;
                }

                // Required scalars
                var propertyGuidStr = RequiredScalar(root, "propertyGuid", filePath);
                var optionText = RequiredScalar(root, "optionText", filePath);

                // Optional Chinese option text
                TryGetScalar(root, "chineseOptionText", out var chineseOptionTextRaw);
                var chineseOptionText = string.IsNullOrWhiteSpace(chineseOptionTextRaw)
                    ? null
                    : chineseOptionTextRaw;

                // flags:
                //   isDefault, isHidden, isOverridden
                var isDefault = false;
                var isHidden = false;
                var isOverridden = false;

                if (TryGetMap(root, "flags", out var flags))
                {
                    isDefault = GetBool(flags, "isDefault", defaultValue: false);
                    isHidden = GetBool(flags, "isHidden", defaultValue: false);
                    isOverridden = GetBool(flags, "isOverridden", defaultValue: false);
                }

                var propertyOption = new PropertyOption
                {
                    //PropertyGuid = G(propertyGuidStr, "propertyGuid", filePath),

                    Guid = Guid.NewGuid(), // no GUID in YAML; generated here
                    IsDefault = isDefault,
                    IsHidden = isHidden,
                    IsOverridden = isOverridden,
                    OptionText = optionText,
                    ChineseOptionText = chineseOptionText
                };

                return propertyOption;
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

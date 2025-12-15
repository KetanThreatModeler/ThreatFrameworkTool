using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ThreatFramework.Infrastructure.YamlRepository;
using ThreatModeler.TF.Core.Model.Global;
using ThreatModeler.TF.Infra.Contract.YamlRepository.Global;
using YamlDotNet.RepresentationModel;

namespace ThreatModeler.TF.Infra.Implmentation.YamlRepository.Global
{
    public class YamlPropertyTypeReader : YamlReaderBase, IYamlPropertyTypeReader
    {
        private readonly ILogger<YamlPropertyTypeReader> _logger;

        private const string EntityDisplayName = "PropertyType";

        public YamlPropertyTypeReader(ILogger<YamlPropertyTypeReader>? logger = null)
        {
            _logger = logger ?? NullLogger<YamlPropertyTypeReader>.Instance;
        }

        public Task<PropertyType> GetPropertyTypeFromFileAsync(string yamlFilePath)
            => LoadYamlEntityAsync(
                yamlFilePath,
                _logger,
                ParsePropertyType,
                EntityDisplayName,
                cancellationToken: default);

        public async Task<IEnumerable<PropertyType>> GetPropertyTypesFromFilesAsync(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null)
                throw new ArgumentNullException(nameof(yamlFilePaths));

            var propertyTypes = new List<PropertyType>();

            foreach (var file in yamlFilePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    var yaml = await TryReadYamlFileAsync(file, _logger, cancellationToken: default);
                    if (yaml is null)
                    {
                        // TryReadYamlFileAsync already logged
                        continue;
                    }

                    var propertyType = ParsePropertyType(yaml, file);
                    if (propertyType is not null)
                    {
                        propertyTypes.Add(propertyType);
                    }
                    // If null, ParsePropertyType already logged why it was skipped.
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

            return propertyTypes;
        }

        #region Parsing

        /// <summary>
        /// Parses a single PropertyType from YAML content.
        /// Returns null if the document is not a 'property-type' kind or is malformed.
        /// For single-file API, LoadYamlEntityAsync will treat null as an error and throw.
        /// </summary>
        private PropertyType? ParsePropertyType(string yaml, string filePath)
        {
            try
            {
                // Validate kind
                var kind = ReadKind(yaml);
                if (!string.Equals(kind, "property-type", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug(
                        "Skipping non-property-type YAML (kind: {Kind}) in {File}",
                        kind ?? "<null>",
                        filePath);

                    return null;
                }

                // Root must be a mapping
                if (!TryLoadRoot(yaml, out var root))
                {
                    _logger.LogWarning(
                        "YAML root is not a mapping. Skipping: {File}",
                        filePath);

                    return null;
                }

                // Required scalars based on your sample YAML
                var guidStr = RequiredScalar(root, "guid", filePath);
                var name = RequiredScalar(root, "name", filePath);

                var propertyType = new PropertyType
                {
                    Guid = G(guidStr, "guid", filePath),
                    Name = name
                };

                return propertyType;
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

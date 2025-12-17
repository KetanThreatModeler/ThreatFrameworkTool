using Microsoft.Extensions.Logging;
using ThreatFramework.Infrastructure.YamlRepository;
using ThreatModeler.TF.Core.Model.PropertyMapping;
using ThreatModeler.TF.Infra.Contract.YamlRepository;
using ThreatModeler.TF.Infra.Contract.YamlRepository.Mappings;
using YamlDotNet.RepresentationModel;

namespace ThreatModeler.TF.Infra.Implmentation.YamlRepository.Mappings
{
    public class YamlComponentPropertyReader : YamlReaderBase, IYamlComponentPropertyReader
    {
        private readonly ILogger<YamlComponentPropertyReader> _logger;

        private const string EntityDisplayName = "ComponentProperty";
        private const string EntitySubFolder = YamlFolderConstants.ComponentPropertyFolder;

        public YamlComponentPropertyReader(ILogger<YamlComponentPropertyReader> logger)
            => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public Task<List<ComponentPropertyMapping>> GetAllAsync(
            string rootFolderPath,
            CancellationToken cancellationToken = default)
            => LoadYamlEntitiesFromFolderAsync(
                rootFolderPath,
                EntitySubFolder,
                _logger,
                ParseComponentProperty,
                EntityDisplayName,
                cancellationToken);

        public Task<ComponentPropertyMapping> GetFromFileAsync(string yamlFilePath)
            => LoadYamlEntityAsync(
                yamlFilePath,
                _logger,
                ParseComponentProperty,
                EntityDisplayName,
                CancellationToken.None);

        #region Parsing

        private ComponentPropertyMapping? ParseComponentProperty(string yaml, string filePath)
        {
            try
            {
                // Root is the spec for this YAML format
                if (!TryLoadRoot(yaml, out var root))
                {
                    _logger.LogWarning(
                        "Unable to load YAML root for {Entity}. File skipped: {File}",
                        EntityDisplayName,
                        filePath);

                    return null;
                }

                var componentGuidStr = RequiredScalar(root, "componentGuid", filePath);
                var propertyGuidStr = RequiredScalar(root, "propertyGuid", filePath);

                var isOptional = GetFlag(root, "isOptional", defaultValue: false);
                var isHidden = GetFlag(root, "isHidden", defaultValue: false);
                var isOverridden = GetFlag(root, "isOverridden", defaultValue: false);

                return new ComponentPropertyMapping
                {
                    Id = 0,
                    ComponentGuid = G(componentGuidStr, "componentGuid", filePath),
                    PropertyGuid = G(propertyGuidStr, "propertyGuid", filePath),
                    IsOptional = isOptional,
                    IsHidden = isHidden,
                    IsOverridden = isOverridden
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Parsing YAML cancelled: {File}", filePath);
                throw;
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

        /// <summary>
        /// Reads boolean flags under root.flags.flagName.
        /// </summary>
        private static bool GetFlag(YamlMappingNode root, string flagName, bool defaultValue)
        {
            if (TryGetMap(root, "flags", out var flagsMap))
            {
                return GetBool(flagsMap, flagName, defaultValue);
            }

            // If flags block is missing (malformed file), just use the default.
            return defaultValue;
        }

        #endregion
    }
}

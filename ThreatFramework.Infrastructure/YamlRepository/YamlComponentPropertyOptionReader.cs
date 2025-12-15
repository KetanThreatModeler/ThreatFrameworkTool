using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract.YamlRepository;
using ThreatModeler.TF.Core.Model.PropertyMapping;
using ThreatModeler.TF.Infra.Contract.YamlRepository;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public class YamlComponentPropertyOptionReader : YamlReaderBase, IYamlComponentPropertyOptionReader
    {
        private readonly ILogger<YamlComponentPropertyOptionReader> _logger;

        private const string EntityDisplayName = "ComponentPropertyOption";
        private const string EntitySubFolder = YamlFolderConstants.ComponentPropertyOptionFolder;

        public YamlComponentPropertyOptionReader(ILogger<YamlComponentPropertyOptionReader> logger)
            => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public Task<List<ComponentPropertyOptionMapping>> GetAllAsync(
            string rootFolderPath,
            CancellationToken cancellationToken = default)
            => LoadYamlEntitiesFromFolderAsync(
                rootFolderPath,
                EntitySubFolder,
                _logger,
                ParseComponentPropertyOption,
                EntityDisplayName,
                cancellationToken);

        public Task<ComponentPropertyOptionMapping> GetFromFileAsync(string yamlFilePath)
            => LoadYamlEntityAsync(
                yamlFilePath,
                _logger,
                ParseComponentPropertyOption,
                EntityDisplayName,
                CancellationToken.None);

        #region Parsing

        private ComponentPropertyOptionMapping? ParseComponentPropertyOption(string yaml, string filePath)
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
                var propertyOptionGuidStr = RequiredScalar(root, "propertyOptionGuid", filePath);

                var isDefault = GetFlag(root, "isDefault", defaultValue: false);
                var isHidden = GetFlag(root, "isHidden", defaultValue: false);
                var isOverridden = GetFlag(root, "isOverridden", defaultValue: false);

                return new ComponentPropertyOptionMapping
                {
                    Id = 0,
                    ComponentGuid = G(componentGuidStr, "componentGuid", filePath),
                    PropertyGuid = G(propertyGuidStr, "propertyGuid", filePath),
                    PropertyOptionGuid = G(propertyOptionGuidStr, "propertyOptionGuid", filePath),
                    IsDefault = isDefault,
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
                _logger.LogWarning(ex,
                    "Failed to parse {Entity} YAML file: {File}",
                    EntityDisplayName,
                    filePath);

                return null;
            }
        }

        /// <summary>
        /// Reads boolean flags under root.flags.flagName
        /// </summary>
        private static bool GetFlag(YamlMappingNode root, string flagName, bool defaultValue)
        {
            // flags: { ... }
            if (TryGetMap(root, "flags", out var flagsMap))
            {
                return GetBool(flagsMap, flagName, defaultValue);
            }

            return defaultValue;
        }

        #endregion
    }
}

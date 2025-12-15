using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.YamlRepository;
using ThreatModeler.TF.Core.Model.PropertyMapping;
using ThreatModeler.TF.Infra.Contract.YamlRepository;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public sealed class YamlCpoThreatReader : YamlReaderBase, IYamlComponentPropertyOptionThreatReader
    {
        private readonly ILogger<YamlCpoThreatReader> _logger;

        private const string EntityDisplayName = "ComponentPropertyOptionThreat";
        private const string EntitySubFolder = YamlFolderConstants.ComponentPropertyOptionThreatFolder;

        public YamlCpoThreatReader(ILogger<YamlCpoThreatReader> logger)
            => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public Task<List<ComponentPropertyOptionThreatMapping>> GetAllAsync(
            string rootFolderPath,
            CancellationToken ct = default)
            => LoadYamlEntitiesFromFolderAsync(
                rootFolderPath,
                EntitySubFolder,
                _logger,
                ParseComponentPropertyOptionThreat,
                EntityDisplayName,
                ct);

        public Task<ComponentPropertyOptionThreatMapping> GetFromFileAsync(string yamlFilePath)
            => LoadYamlEntityAsync(
                yamlFilePath,
                _logger,
                ParseComponentPropertyOptionThreat,
                EntityDisplayName,
                CancellationToken.None);

        #region Parsing

        private ComponentPropertyOptionThreatMapping? ParseComponentPropertyOptionThreat(
            string yaml,
            string filePath)
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
                var threatGuidStr = RequiredScalar(root, "threatGuid", filePath);

                var isHidden = GetFlag(root, "isHidden", defaultValue: false);
                var isOverridden = GetFlag(root, "isOverridden", defaultValue: false);

                return new ComponentPropertyOptionThreatMapping
                {
                    Id = 0,
                    ComponentGuid = G(componentGuidStr, "componentGuid", filePath),
                    PropertyGuid = G(propertyGuidStr, "propertyGuid", filePath),
                    PropertyOptionGuid = G(propertyOptionGuidStr, "propertyOptionGuid", filePath),
                    ThreatGuid = G(threatGuidStr, "threatGuid", filePath),
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

            return defaultValue;
        }

        #endregion
    }
}

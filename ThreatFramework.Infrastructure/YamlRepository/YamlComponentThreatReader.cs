using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.YamlRepository;
using ThreatModeler.TF.Core.Model.ComponentMapping;
using ThreatModeler.TF.Infra.Contract.YamlRepository;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public class YamlComponentThreatReader : YamlReaderBase, IYamlComponentThreatReader
    {
        private readonly ILogger<YamlComponentThreatReader> _logger;

        private const string EntityDisplayName = "ComponentThreat";
        private const string EntitySubFolder = YamlFolderConstants.ComponentThreatFolder;

        public YamlComponentThreatReader(ILogger<YamlComponentThreatReader> logger)
            => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public Task<List<ComponentThreatMapping>> GetAllAsync(
            string rootFolderPath,
            CancellationToken cancellationToken = default)
            => LoadYamlEntitiesFromFolderAsync(
                rootFolderPath,
                EntitySubFolder,
                _logger,
                ParseComponentThreat,
                EntityDisplayName,
                cancellationToken);

        public Task<ComponentThreatMapping> GetFromFileAsync(string yamlFilePath)
            => LoadYamlEntityAsync(
                yamlFilePath,
                _logger,
                ParseComponentThreat,
                EntityDisplayName,
                CancellationToken.None);

        #region Parsing

        private ComponentThreatMapping? ParseComponentThreat(string yaml, string filePath)
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
                var threatGuidStr = RequiredScalar(root, "threatGuid", filePath);

                var isHidden = GetFlag(root, "isHidden", defaultValue: false);
                var isOverridden = GetFlag(root, "isOverridden", defaultValue: false);
                var usedForMitigation = GetFlag(root, "usedForMitigation", defaultValue: false);

                return new ComponentThreatMapping
                {
                    ComponentGuid = G(componentGuidStr, "componentGuid", filePath),
                    ThreatGuid = G(threatGuidStr, "threatGuid", filePath),
                    IsHidden = isHidden,
                    IsOverridden = isOverridden,
                    UsedForMitigation = usedForMitigation
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

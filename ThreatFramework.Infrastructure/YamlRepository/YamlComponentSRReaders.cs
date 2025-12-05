using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThreatFramework.Core.ComponentMapping;
using ThreatFramework.Infra.Contract.YamlRepository;
using ThreatModeler.TF.Infra.Contract.YamlRepository;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public class YamlComponentSRReaders : YamlReaderBase, IYamlComponentSRReader
    {
        private readonly ILogger<YamlComponentSRReaders> _logger;

        private const string EntityDisplayName = "ComponentSecurityRequirement";
        private const string EntitySubFolder = YamlFolderConstants.ComponentSecurityRequirementFolder;

        public YamlComponentSRReaders(ILogger<YamlComponentSRReaders> logger)
            => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public Task<List<ComponentSecurityRequirementMapping>> GetAllComponentSRAsync(
            string rootFolderPath,
            CancellationToken ct = default)
            => LoadYamlEntitiesFromFolderAsync(
                rootFolderPath,
                EntitySubFolder,
                _logger,
                ParseComponentSecurityRequirement,
                EntityDisplayName,
                ct);

        public Task<ComponentSecurityRequirementMapping> GetComponentSRFromFileAsync(string yamlFilePath)
            => LoadYamlEntityAsync(
                yamlFilePath,
                _logger,
                ParseComponentSecurityRequirement,
                EntityDisplayName,
                CancellationToken.None);

        #region Parsing

        private ComponentSecurityRequirementMapping? ParseComponentSecurityRequirement(string yaml, string filePath)
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
                var securityRequirementGuidStr = RequiredScalar(root, "securityRequirementGuid", filePath);

                var isHidden = GetFlag(root, "isHidden", defaultValue: false);
                var isOverridden = GetFlag(root, "isOverridden", defaultValue: false);

                return new ComponentSecurityRequirementMapping
                {
                    ComponentGuid = G(componentGuidStr, "componentGuid", filePath),
                    SecurityRequirementGuid = G(securityRequirementGuidStr, "securityRequirementGuid", filePath),
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

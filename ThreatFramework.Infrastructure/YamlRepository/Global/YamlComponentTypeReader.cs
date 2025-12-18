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
    public class YamlComponentTypeReader : YamlReaderBase, IYamlComponentTypeReader
    {
        private readonly ILogger<YamlComponentTypeReader> _logger;

        private const string EntityDisplayName = "ComponentType";

        public YamlComponentTypeReader(ILogger<YamlComponentTypeReader>? logger = null)
        {
            _logger = logger ?? NullLogger<YamlComponentTypeReader>.Instance;
        }

        public Task<ComponentType> GetComponentTypeFromFileAsync(string yamlFilePath)
            => LoadYamlEntityAsync(
                yamlFilePath,
                _logger,
                ParseComponentType,
                EntityDisplayName,
                cancellationToken: default);

        public async Task<IEnumerable<ComponentType>> GetComponentTypesFromFilesAsync(
            IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null)
            {
                throw new ArgumentNullException(nameof(yamlFilePaths));
            }

            var componentTypes = new List<ComponentType>();

            foreach (var file in yamlFilePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    // DRY: use shared IO helper from YamlReaderBase
                    var yaml = await TryReadYamlFileAsync(file, _logger, cancellationToken: default);
                    if (yaml is null)
                    {
                        // Warning already logged in TryReadYamlFileAsync
                        continue;
                    }

                    var componentType = ParseComponentType(yaml, file);
                    if (componentType is not null)
                    {
                        componentTypes.Add(componentType);
                    }
                    // If null, ParseComponentType already logged reason (kind mismatch, malformed YAML, etc.).
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

            return componentTypes;
        }

        #region Parsing

        /// <summary>
        /// Parses a single ComponentType from YAML content.
        /// Returns null if the document is not a 'component-type' (when kind is present)
        /// or if the YAML is malformed.
        /// For single-file API, LoadYamlEntityAsync will treat null as an error and throw.
        /// </summary>
        private ComponentType? ParseComponentType(string yaml, string filePath)
        {
            try
            {
                // Optional kind: if present and not "component-type", skip.
                var kind = ReadKind(yaml);
                if (!string.IsNullOrWhiteSpace(kind) &&
                    !string.Equals(kind, "component-type", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug(
                        "Skipping non-component-type YAML (kind: {Kind}) in {File}",
                        kind,
                        filePath);

                    return null;
                }

                // Root is the spec for ComponentType YAML
                if (!TryLoadRoot(yaml, out var root))
                {
                    _logger.LogWarning(
                        "YAML root is not a mapping. Skipping: {File}",
                        filePath);

                    return null;
                }

                // Required top-level scalars
                var guidStr = RequiredScalar(root, "guid", filePath);
                var libraryGuidStr = RequiredScalar(root, "libraryGuid", filePath);
                var name = RequiredScalar(root, "name", filePath);

                // Optional description
                TryGetScalar(root, "description", out var descriptionRaw);
                var description = string.IsNullOrWhiteSpace(descriptionRaw)
                    ? null
                    : System.Net.WebUtility.HtmlDecode(descriptionRaw);

                // chineseName / chineseDescription
                TryGetScalar(root, "chineseName", out var chineseNameRaw);
                var chineseName = string.IsNullOrWhiteSpace(chineseNameRaw)
                    ? null
                    : chineseNameRaw;

                TryGetScalar(root, "chineseDescription", out var chineseDescRaw);
                var chineseDescription = string.IsNullOrWhiteSpace(chineseDescRaw)
                    ? null
                    : System.Net.WebUtility.HtmlDecode(chineseDescRaw);

                // flags:
                //   isHidden: false
                //   isSecurityControl: false
                var isHidden = false;
                var isSecurityControl = false;

                if (TryGetMap(root, "flags", out var flags))
                {
                    isHidden = GetBool(flags, "isHidden", defaultValue: false);
                    isSecurityControl = GetBool(flags, "isSecurityControl", defaultValue: false);
                }

                var componentType = new ComponentType
                {
                    // Assuming these properties exist on ComponentType; adjust if your model differs
                    Guid = G(guidStr, "guid", filePath),
                    LibraryGuid = G(libraryGuidStr, "libraryGuid", filePath),
                    Name = name,
                    Description = description,
                    ChineseName = chineseName,
                    ChineseDescription = chineseDescription,
                    IsHidden = isHidden,
                    IsSecurityControl = isSecurityControl
                };

                return componentType;
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

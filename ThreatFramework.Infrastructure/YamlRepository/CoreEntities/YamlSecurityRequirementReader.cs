using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository.CoreEntities
{
    public class YamlSecurityRequirementReader : YamlReaderBase, IYamlSecurityRequirementReader
    {
        private readonly ILogger<YamlSecurityRequirementReader> _logger;

        private const string EntityDisplayName = "SecurityRequirement";

        public YamlSecurityRequirementReader(ILogger<YamlSecurityRequirementReader>? logger = null)
        {
            _logger = logger ?? NullLogger<YamlSecurityRequirementReader>.Instance;
        }

        public Task<SecurityRequirement> GetSecurityRequirementFromFileAsync(string yamlFilePath)
            => LoadYamlEntityAsync(
                yamlFilePath,
                _logger,
                ParseSecurityRequirement,
                EntityDisplayName,
                cancellationToken: default);

        /// <summary>
        /// Parse a set of YAML files into SecurityRequirement entities.
        /// Only files with kind: security-requirement are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<SecurityRequirement>> GetSecurityRequirementsFromFilesAsync(
            IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null)
            {
                throw new ArgumentNullException(nameof(yamlFilePaths));
            }

            var securityRequirements = new List<SecurityRequirement>();

            foreach (var file in yamlFilePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    // DRY: shared IO helper (handles missing file + read errors + logging)
                    var yaml = await TryReadYamlFileAsync(file, _logger, cancellationToken: default);
                    if (yaml is null)
                    {
                        // Warning already logged by TryReadYamlFileAsync
                        continue;
                    }

                    var securityRequirement = ParseSecurityRequirement(yaml, file);
                    if (securityRequirement is not null)
                    {
                        securityRequirements.Add(securityRequirement);
                    }
                    // If null, ParseSecurityRequirement has already logged the reason.
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

            return securityRequirements;
        }

        #region Parsing

        /// <summary>
        /// Parses a single SecurityRequirement from YAML content.
        /// Returns null if the document is not a 'security-requirement' kind or is malformed.
        /// For single-file API, LoadYamlEntityAsync will treat null as an error and throw.
        /// </summary>
        private SecurityRequirement? ParseSecurityRequirement(string yaml, string filePath)
        {
            try
            {
                // Validate kind
                var kind = ReadKind(yaml);
                if (!string.Equals(kind, "security-requirement", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug(
                        "Skipping non-security-requirement YAML (kind: {Kind}) in {File}",
                        kind ?? "<null>",
                        filePath);

                    return null;
                }

                // Root is the spec for SecurityRequirement YAML
                if (!TryLoadRoot(yaml, out var root))
                {
                    _logger.LogWarning("YAML root is not a mapping. Skipping: {File}", filePath);
                    return null;
                }

                // Required top-level scalars
                var guidStr = RequiredScalar(root, "guid", filePath);
                var name = RequiredScalar(root, "name", filePath);
                var libraryGuidStr = RequiredScalar(root, "libraryGuid", filePath);

                var riskName = RequiredScalar(root, "riskName", filePath);

                // labels: sequence -> comma-separated string (null if empty)
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

                // flags
                var isCompensatingControl = false;
                var isHidden = false;
                var isOverridden = false;

                if (TryGetMap(root, "flags", out var flags))
                {
                    isCompensatingControl = GetBool(flags, "isCompensatingControl", false);
                    isHidden = GetBool(flags, "isHidden", false);
                    isOverridden = GetBool(flags, "isOverridden", false);
                }

                var securityRequirement = new SecurityRequirement
                {
                    // now correctly a string
                    RiskName = riskName,
                    Guid = G(guidStr, "guid", filePath),
                    LibraryId = G(libraryGuidStr, "libraryGuid", filePath),
                    IsCompensatingControl = isCompensatingControl,
                    IsHidden = isHidden,
                    IsOverridden = isOverridden,
                    Name = name,
                    ChineseName = chineseName,
                    Labels = labels,
                    Description = description,
                    ChineseDescription = chineseDescription
                };

                return securityRequirement;
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

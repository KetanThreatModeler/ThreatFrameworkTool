using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        #region Public API

        public async Task<SecurityRequirement> GetSecurityRequirementFromFileAsync(string yamlFilePath)
        {
            if (string.IsNullOrWhiteSpace(yamlFilePath))
                throw new ArgumentException("YAML file path must not be null or empty.", nameof(yamlFilePath));

            // 1. Read raw YAML
            var yaml = await TryReadYamlFileAsync(yamlFilePath, _logger, cancellationToken: default);
            if (yaml is null)
            {
                throw new InvalidOperationException(
                    $"Could not read YAML file for {EntityDisplayName}: '{yamlFilePath}'.");
            }

            // 2. Sanitize invalid backslashes inside double-quoted scalars
            yaml = SanitizeInvalidEscapesInDoubleQuotedScalars(yaml);

            try
            {
                // 3. Parse strongly
                return ParseSecurityRequirementStrict(yaml, yamlFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to parse {Entity} from YAML file {File}. Raw YAML (sanitized):{NewLine}{Yaml}",
                    EntityDisplayName,
                    yamlFilePath,
                    Environment.NewLine,
                    yaml);

                throw new InvalidOperationException(
                    $"Failed to parse {EntityDisplayName} from YAML file '{yamlFilePath}'. See inner exception for details.",
                    ex);
            }
        }

        /// <summary>
        /// Parse a set of YAML files into SecurityRequirement entities.
        /// Only files with kind: security-requirement are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<SecurityRequirement>> GetSecurityRequirementsFromFilesAsync(
            IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null)
                throw new ArgumentNullException(nameof(yamlFilePaths));

            var securityRequirements = new List<SecurityRequirement>();

            foreach (var file in yamlFilePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    var yaml = await TryReadYamlFileAsync(file, _logger, cancellationToken: default);
                    if (yaml is null)
                    {
                        continue;
                    }

                    yaml = SanitizeInvalidEscapesInDoubleQuotedScalars(yaml);

                    var sr = ParseSecurityRequirementStrict(yaml, file);
                    securityRequirements.Add(sr);
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

        #endregion

        #region Sanitization helper (same pattern as Threat reader)

        /// <summary>
        /// Workaround for bad YAML: inside double-quoted strings, backslashes must either:
        ///   - start a valid YAML escape (\n, \t, \", \\, \xNN, \uNNNN, \UNNNNNNNN, etc.), or
        ///   - be escaped themselves.
        ///
        /// Your data has things like "c:\Program Files\" or "%temp%\download"
        /// which produce "unknown escape character" errors.
        ///
        /// This method walks the raw YAML text and, for any backslash found
        /// inside a double-quoted scalar that is *not* a valid escape,
        /// it inserts an extra '\' so YamlDotNet treats it as a literal backslash.
        /// </summary>
        private static string SanitizeInvalidEscapesInDoubleQuotedScalars(string yaml)
        {
            if (string.IsNullOrEmpty(yaml))
                return yaml;

            var sb = new StringBuilder(yaml.Length + 64);
            bool inQuotes = false;

            for (int i = 0; i < yaml.Length; i++)
            {
                char c = yaml[i];

                // Toggle quote state (ignore escaped quotes)
                if (c == '"' && (i == 0 || yaml[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                    sb.Append(c);
                    continue;
                }

                if (inQuotes && c == '\\')
                {
                    if (i + 1 < yaml.Length)
                    {
                        char next = yaml[i + 1];

                        const string validEscapes = "0abtnvfre \"\\N_LPxuU";

                        bool isValidEscape = validEscapes.IndexOf(next) >= 0;

                        if (!isValidEscape)
                        {
                            // Insert extra '\' to turn \X into \\X
                            sb.Append('\\');
                        }
                    }
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Strict parsing of a single SecurityRequirement from YAML content.
        /// Throws a clear exception if malformed.
        /// </summary>
        private SecurityRequirement ParseSecurityRequirementStrict(string yaml, string filePath)
        {
            var stream = new YamlStream();
            using (var reader = new StringReader(yaml))
            {
                stream.Load(reader);
            }

            if (stream.Documents.Count == 0 ||
                stream.Documents[0].RootNode is not YamlMappingNode root)
            {
                throw new FormatException($"YAML root is not a mapping in file '{filePath}'.");
            }

            // Validate kind (warn if not security-requirement but keep going)
            if (root.Children.TryGetValue(new YamlScalarNode("kind"), out var kindNode) &&
                kindNode is YamlScalarNode kindScalar)
            {
                var kindVal = kindScalar.Value;
                if (!string.Equals(kindVal, "security-requirement", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "YAML file '{File}' has unexpected kind '{Kind}' (expected 'security-requirement'). Continuing to parse anyway.",
                        filePath,
                        kindVal ?? "<null>");
                }
            }

            // Required scalars
            var guidStr = RequiredScalar(root, "guid", filePath);
            var name = RequiredScalar(root, "name", filePath);
            var libraryGuidStr = RequiredScalar(root, "libraryGuid", filePath);

            // riskName OR RiskName
            string riskName;
            if (!TryGetScalar(root, "riskName", out var riskNameRaw) ||
                string.IsNullOrWhiteSpace(riskNameRaw))
            {
                riskName = RequiredScalar(root, "RiskName", filePath);
            }
            else
            {
                riskName = riskNameRaw!;
            }

            // labels: sequence -> comma-separated
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

            // description (HTML encoded allowed)
            TryGetScalar(root, "description", out var descriptionRaw);
            var description = string.IsNullOrWhiteSpace(descriptionRaw)
                ? null
                : System.Net.WebUtility.HtmlDecode(descriptionRaw);

            // ChineseName / ChineseDescription (note YAML casing)
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

            if (root.Children.TryGetValue(new YamlScalarNode("flags"), out var flagsNode)
                && flagsNode is YamlMappingNode flags)
            {
                isCompensatingControl = GetBool(flags, "isCompensatingControl", false);
                isHidden = GetBool(flags, "isHidden", false);
                isOverridden = GetBool(flags, "isOverridden", false);
            }

            return new SecurityRequirement
            {
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
        }

        #endregion
    }
}

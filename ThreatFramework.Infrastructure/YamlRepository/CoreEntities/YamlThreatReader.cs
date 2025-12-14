using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using ThreatModeler.TF.Core.Helper;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository.CoreEntities
{
    public class YamlThreatReader : YamlReaderBase, IYamlThreatReader
    {
        private readonly ILogger<YamlThreatReader> _logger;
        private const string EntityDisplayName = "Threat";

        public YamlThreatReader(ILogger<YamlThreatReader>? logger = null)
        {
            _logger = logger ?? NullLogger<YamlThreatReader>.Instance;
        }

        public async Task<Threat> GetThreatFromFileAsync(string yamlFilePath)
        {
            if (string.IsNullOrWhiteSpace(yamlFilePath))
                throw new ArgumentException("YAML file path must not be null or empty.", nameof(yamlFilePath));

            var yaml = await TryReadYamlFileAsync(yamlFilePath, _logger, cancellationToken: default);
            if (yaml is null)
            {
                throw new InvalidOperationException(
                    $"Could not read YAML file for {EntityDisplayName}: '{yamlFilePath}'.");
            }

            // 🔧 Fix invalid backslashes inside double-quoted scalars *before* parsing
            yaml = SanitizeInvalidEscapesInDoubleQuotedScalars(yaml);

            try
            {
                return ParseThreatStrict(yaml, yamlFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to parse {Entity} from YAML file {File}. Raw YAML (sanitized):\n{Yaml}",
                    EntityDisplayName,
                    yamlFilePath,
                    yaml);

                throw new InvalidOperationException(
                    $"Failed to parse {EntityDisplayName} from YAML file '{yamlFilePath}'. See inner exception for details.",
                    ex);
            }
        }

        public async Task<IEnumerable<Threat>> GetThreatsFromFilesAsync(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null)
                throw new ArgumentNullException(nameof(yamlFilePaths));

            var threats = new List<Threat>();

            foreach (var file in yamlFilePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    var yaml = await TryReadYamlFileAsync(file, _logger, cancellationToken: default);
                    if (yaml is null)
                    {
                        continue;
                    }

                    // 🔧 Sanitize before parsing
                    yaml = SanitizeInvalidEscapesInDoubleQuotedScalars(yaml);

                    var threat = ParseThreatStrict(yaml, file);
                    threats.Add(threat);
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

            return threats;
        }

        /// <summary>
        /// Workaround for bad YAML: inside double-quoted strings, backslashes must either:
        ///   - start a valid YAML escape (\n, \t, \", \\, \xNN, \uNNNN, \UNNNNNNNN, etc.), or
        ///   - be escaped themselves.
        ///
        /// Your data has things like "c:\Program Files\" and "%temp%\download"
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
                    // We're inside a double-quoted scalar and see a backslash.
                    // Decide if it's a valid escape; if not, double it to make it literal.
                    if (i + 1 < yaml.Length)
                    {
                        char next = yaml[i + 1];

                        // Characters that YAML considers valid simple escapes
                        const string validSimpleEscapes = "0abtnvfre \"\\N_LP";

                        bool isSimpleValid = validSimpleEscapes.IndexOf(next) >= 0;

                        if (!isSimpleValid && next != 'x' && next != 'u' && next != 'U')
                        {
                            // Like \C, \q etc. -> make it \\C, \\q etc.
                            sb.Append('\\');
                            sb.Append('\\');
                            continue; // next loop iteration will handle the char after '\'
                        }
                    }
                }

                sb.Append(c);
            }

            var sanitized = sb.ToString();

            // EXTRA PASS: fix malformed \x, \u, \U escapes

            // \x must be followed by exactly 2 hex digits to be valid.
            // If not, change \x -> \\x so it's literal text.
            sanitized = Regex.Replace(
                sanitized,
                @"\\x(?![0-9a-fA-F]{2})",
                @"\\x");

            // \u must be followed by exactly 4 hex digits.
            sanitized = Regex.Replace(
                sanitized,
                @"\\u(?![0-9a-fA-F]{4})",
                @"\\u");

            // \U must be followed by exactly 8 hex digits.
            sanitized = Regex.Replace(
                sanitized,
                @"\\U(?![0-9a-fA-F]{8})",
                @"\\U");

            return sanitized;
        }


        /// <summary>
        /// Strict parsing – any error throws with a clear message.
        /// </summary>
        private Threat ParseThreatStrict(string yaml, string filePath)
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

            // kind – expect "threat"
            if (root.Children.TryGetValue(new YamlScalarNode("kind"), out var kindNode) &&
                kindNode is YamlScalarNode kindScalar)
            {
                var kindVal = kindScalar.Value;
                if (!string.Equals(kindVal, "threat", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "YAML file '{File}' has unexpected kind '{Kind}' (expected 'threat'). Continuing to parse anyway.",
                        filePath,
                        kindVal ?? "<null>");
                }
            }

            // Required scalars
            string guidStr = RequiredScalar(root, "guid", filePath);
            string name = RequiredScalar(root, "name", filePath);
            string libraryGuidStr = RequiredScalar(root, "libraryGuid", filePath);

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

            // labels as list
            var labelsList = new List<string>();
            if (root.Children.TryGetValue(new YamlScalarNode("labels"), out var labelsNode) &&
                labelsNode is YamlSequenceNode labelsSeq)
            {
                labelsList.AddRange(
                    labelsSeq.Children
                        .OfType<YamlScalarNode>()
                        .Where(n => !string.IsNullOrWhiteSpace(n.Value))
                        .Select(n => n.Value!.Trim()));
            }

            // Optional scalars
            //TryGetScalar(root, "description", out var descriptionRaw);
            /*var description = string.IsNullOrWhiteSpace(descriptionRaw)
                ? string.Empty
                : System.Net.WebUtility.HtmlDecode(descriptionRaw);*/

            TryGetScalar(root, "reference", out var referenceRaw);
            var reference = referenceRaw ?? string.Empty;

            TryGetScalar(root, "intelligence", out var intelRaw);
            var intelligence = intelRaw ?? string.Empty;

            TryGetScalar(root, "chineseName", out var cnRaw);
            var chineseName = cnRaw ?? string.Empty;

            TryGetScalar(root, "chineseDescription", out var cdRaw);
            var chineseDescription = string.IsNullOrWhiteSpace(cdRaw)
                ? string.Empty
                : System.Net.WebUtility.HtmlDecode(cdRaw);

            // Flags
            var automated = false;
            var isHidden = false;
            var isOverridden = false;

            if (root.Children.TryGetValue(new YamlScalarNode("flags"), out var flagsNode) &&
                flagsNode is YamlMappingNode flagsMap)
            {
                automated = GetBool(flagsMap, "automated", false);
                isHidden = GetBool(flagsMap, "isHidden", false);
                isOverridden = GetBool(flagsMap, "isOverridden", false);
            }

            return new Threat
            {
                RiskName = riskName,
                Guid = G(guidStr, "guid", filePath),
                LibraryGuid = G(libraryGuidStr, "libraryGuid", filePath),
                Automated = automated,
                IsHidden = isHidden,
                IsOverridden = isOverridden,
                Name = name,
                ChineseName = chineseName,
                Labels = labelsList,
                Description = string.Empty,
                Reference = reference,
                Intelligence = intelligence,
                ChineseDescription = chineseDescription
            };
        }
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public Task<Threat> GetThreatFromFileAsync(string yamlFilePath)
            => LoadYamlEntityAsync(
                yamlFilePath,
                _logger,
                ParseThreat,
                EntityDisplayName,
                cancellationToken: default);

        /// <summary>
        /// Parse a set of YAML files into Threat entities.
        /// Only files with kind: threat are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<Threat>> GetThreatsFromFilesAsync(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null)
            {
                throw new ArgumentNullException(nameof(yamlFilePaths));
            }

            var threats = new List<Threat>();

            // Sequential for predictable logging/error flow.
            foreach (var file in yamlFilePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    // DRY: shared IO helper (handles existence + read + logging)
                    var yaml = await TryReadYamlFileAsync(file, _logger, cancellationToken: default);
                    if (yaml is null)
                    {
                        // TryReadYamlFileAsync already logged the reason
                        continue;
                    }

                    var threat = ParseThreat(yaml, file);
                    if (threat is not null)
                    {
                        threats.Add(threat);
                    }
                    // If null, ParseThreat already logged why it was skipped.
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

        #region Parsing

        /// <summary>
        /// Parses a single Threat from YAML content.
        /// Returns null if the document is not a 'threat' kind or is malformed.
        /// For single-file API, LoadYamlEntityAsync will treat null as an error and throw.
        /// </summary>
        private Threat? ParseThreat(string yaml, string filePath)
        {
            try
            {
                // Validate kind
                var kind = ReadKind(yaml);
                if (!string.Equals(kind, "threat", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug(
                        "Skipping non-threat YAML (kind: {Kind}) in {File}",
                        kind ?? "<null>",
                        filePath);

                    return null;
                }

                // Root is the spec for Threat YAML
                if (!TryLoadRoot(yaml, out var root))
                {
                    _logger.LogWarning(
                        "YAML root is not a mapping. Skipping: {File}",
                        filePath);

                    return null;
                }

                // Required top-level scalars
                var guidStr = RequiredScalar(root, "guid", filePath);
                var name = RequiredScalar(root, "name", filePath);
                var libraryGuidStr = RequiredScalar(root, "libraryGuid", filePath);

                var riskName = RequiredScalar(root, "riskName", filePath);

                // labels: sequence -> comma-separated string -> ToListWithTrim()
                string? labelsCsv = null;
                if (root.Children.TryGetValue(new YamlScalarNode("labels"), out var labelsNode) &&
                    labelsNode is YamlSequenceNode seq &&
                    seq.Children.Count > 0)
                {
                    var labelValues = seq.Children
                        .OfType<YamlScalarNode>()
                        .Where(s => !string.IsNullOrWhiteSpace(s.Value))
                        .Select(s => s.Value!.Trim());

                    var joined = string.Join(",", labelValues);
                    labelsCsv = string.IsNullOrWhiteSpace(joined) ? null : joined;
                }

                // description / reference / intelligence
                TryGetScalar(root, "description", out var descriptionRaw);
                var description = string.IsNullOrWhiteSpace(descriptionRaw)
                    ? null
                    : System.Net.WebUtility.HtmlDecode(descriptionRaw);

                TryGetScalar(root, "reference", out var referenceRaw);
                var reference = string.IsNullOrWhiteSpace(referenceRaw)
                    ? null
                    : referenceRaw;

                TryGetScalar(root, "intelligence", out var intelligenceRaw);
                var intelligence = string.IsNullOrWhiteSpace(intelligenceRaw)
                    ? null
                    : intelligenceRaw;

                // chineseName / chineseDescription
                TryGetScalar(root, "chineseName", out var chineseNameRaw);
                var chineseName = string.IsNullOrWhiteSpace(chineseNameRaw)
                    ? null
                    : chineseNameRaw;

                TryGetScalar(root, "chineseDescription", out var chineseDescRaw);
                var chineseDescription = string.IsNullOrWhiteSpace(chineseDescRaw)
                    ? null
                    : System.Net.WebUtility.HtmlDecode(chineseDescRaw);

                // flags
                var automated = false;
                var isHidden = false;
                var isOverridden = false;

                if (TryGetMap(root, "flags", out var flags))
                {
                    automated = GetBool(flags, "automated", false);
                    isHidden = GetBool(flags, "isHidden", false);
                    isOverridden = GetBool(flags, "isOverridden", false);
                }

                var threat = new Threat
                {
                    // Now string RiskName instead of numeric RiskId
                    RiskName = riskName,
                    Guid = G(guidStr, "guid", filePath),
                    LibraryGuid = G(libraryGuidStr, "libraryGuid", filePath),
                    Automated = automated,
                    IsHidden = isHidden,
                    IsOverridden = isOverridden,
                    Name = name,
                    ChineseName = chineseName ?? string.Empty,
                    Labels = labelsCsv.ToListWithTrim(),
                    Description = description ?? string.Empty,
                    Reference = reference ?? string.Empty,
                    Intelligence = intelligence ?? string.Empty,
                    ChineseDescription = chineseDescription ?? string.Empty
                };

                return threat;
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

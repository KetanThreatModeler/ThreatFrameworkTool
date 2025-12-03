using Microsoft.Extensions.Logging;
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
        private readonly ILogger<YamlSecurityRequirementReader>? _logger;

        public YamlSecurityRequirementReader(ILogger<YamlSecurityRequirementReader>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Parse a set of YAML files into SecurityRequirement entities.
        /// Only files with kind: security-requirement are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<SecurityRequirement>> GetSecurityRequirementsFromFilesAsync(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null) throw new ArgumentNullException(nameof(yamlFilePaths));

            var securityRequirements = new List<SecurityRequirement>();

            foreach (var file in yamlFilePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    if (!File.Exists(file))
                    {
                        _logger?.LogWarning("YAML file not found: {File}", file);
                        continue;
                    }

                    string yaml;
                    try
                    {
                        yaml = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                    }
                    catch (Exception readEx)
                    {
                        _logger?.LogWarning(readEx, "Failed to read YAML file: {File}", file);
                        continue;
                    }

                    // Validate kind
                    var kind = ReadKind(yaml);
                    if (!string.Equals(kind, "security-requirement", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogDebug("Skipping non-security-requirement YAML (kind: {Kind}) in {File}", kind ?? "<null>", file);
                        continue;
                    }

                    // Load root mapping (flat structure)
                    var yamlStream = new YamlStream();
                    using (var sr = new StringReader(yaml))
                    {
                        yamlStream.Load(sr);
                    }

                    if (yamlStream.Documents.Count == 0 || yamlStream.Documents[0].RootNode is not YamlMappingNode root)
                    {
                        _logger?.LogWarning("YAML root is not a mapping. Skipping: {File}", file);
                        continue;
                    }

                    // Required top-level scalars
                    var guidStr = RequiredScalar(root, "guid", file);
                    var name = RequiredScalar(root, "name", file);
                    var libraryGuidStr = RequiredScalar(root, "libraryGuid", file);

                    // riskId (present in YAML, Id is not)
                    // try both "riskId" and "riskID" to be safe
                    string riskIdRaw;
                    if (!TryGetScalar(root, "riskId", out riskIdRaw) &&
                        !TryGetScalar(root, "riskID", out riskIdRaw))
                    {
                        throw new InvalidOperationException($"Missing 'riskId' in security requirement YAML: {file}");
                    }

                    if (!int.TryParse(riskIdRaw, out var riskId))
                    {
                        throw new InvalidOperationException($"Invalid 'riskId' value '{riskIdRaw}' in file: {file}");
                    }

                    // labels: sequence -> comma-separated string (null if empty)
                    string? labels = null;
                    if (root.Children.TryGetValue(new YamlScalarNode("labels"), out var labelsNode)
                        && labelsNode is YamlSequenceNode seq && seq.Children.Count > 0)
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
                    string? chineseName = string.IsNullOrWhiteSpace(chineseNameRaw) ? null : chineseNameRaw;

                    TryGetScalar(root, "ChineseDescription", out var chineseDescRaw);
                    string? chineseDescription = string.IsNullOrWhiteSpace(chineseDescRaw)
                        ? null
                        : System.Net.WebUtility.HtmlDecode(chineseDescRaw);

                    // flags
                    bool isCompensatingControl = false, isHidden = false, isOverridden = false;
                    if (TryGetMap(root, "flags", out var flags))
                    {
                        isCompensatingControl = GetBool(flags, "isCompensatingControl", false);
                        isHidden = GetBool(flags, "isHidden", false);
                        isOverridden = GetBool(flags, "isOverridden", false);
                    }

                    var securityRequirement = new SecurityRequirement
                    {
                        RiskId = riskId, // now comes from YAML
                        Guid = G(guidStr, "guid", file),
                        LibraryId = G(libraryGuidStr, "libraryGuid", file),
                        IsCompensatingControl = isCompensatingControl,
                        IsHidden = isHidden,
                        IsOverridden = isOverridden,
                        Name = name,
                        ChineseName = chineseName,
                        Labels = labels,
                        Description = description,
                        ChineseDescription = chineseDescription,
                    };

                    securityRequirements.Add(securityRequirement);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse security requirement from YAML file: {File}", file);
                }
            }

            return securityRequirements;
        }
    }
    }

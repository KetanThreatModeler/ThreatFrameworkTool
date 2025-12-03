using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using ThreatModeler.TF.Core.Helper;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository.CoreEntities
{
    public class YamlThreatReader : YamlReaderBase, IYamlThreatReader
    {
        private readonly ILogger<YamlThreatReader>? _logger;

        public YamlThreatReader(ILogger<YamlThreatReader>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Parse a set of YAML files into Threat entities.
        /// Only files with kind: threat are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<Threat>> GetThreatsFromFilesAsync(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null) throw new ArgumentNullException(nameof(yamlFilePaths));

            var threats = new List<Threat>();

            // We iterate sequentially to keep logging + error flow simple and predictable.
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
                    if (!string.Equals(kind, "threat", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogDebug("Skipping non-threat YAML (kind: {Kind}) in {File}", kind ?? "<null>", file);
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

                    // labels: sequence -> comma-separated string -> ToListWithTrim()
                    string? labelsCsv = null;
                    if (root.Children.TryGetValue(new YamlScalarNode("labels"), out var labelsNode)
                        && labelsNode is YamlSequenceNode seq && seq.Children.Count > 0)
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
                    var reference = string.IsNullOrWhiteSpace(referenceRaw) ? null : referenceRaw;

                    TryGetScalar(root, "intelligence", out var intelligenceRaw);
                    var intelligence = string.IsNullOrWhiteSpace(intelligenceRaw) ? null : intelligenceRaw;

                    // chineseName / chineseDescription (case matches YAML: chineseName, chineseDescription)
                    TryGetScalar(root, "chineseName", out var chineseNameRaw);
                    string? chineseName = string.IsNullOrWhiteSpace(chineseNameRaw) ? null : chineseNameRaw;

                    TryGetScalar(root, "chineseDescription", out var chineseDescRaw);
                    string? chineseDescription = string.IsNullOrWhiteSpace(chineseDescRaw)
                        ? null
                        : System.Net.WebUtility.HtmlDecode(chineseDescRaw);

                    // flags
                    bool automated = false, isHidden = false, isOverridden = false;
                    if (TryGetMap(root, "flags", out var flags))
                    {
                        automated = GetBool(flags, "automated", false);
                        isHidden = GetBool(flags, "isHidden", false);
                        isOverridden = GetBool(flags, "isOverridden", false);
                    }

                    var threat = new Threat
                    {
                        Id = 0, // set by persistence later
                        RiskId = 0, // still not present in YAML; set elsewhere if needed
                        Guid = G(guidStr, "guid", file),
                        LibraryGuid = G(libraryGuidStr, "libraryGuid", file),
                        Automated = automated,
                        IsHidden = isHidden,
                        IsOverridden = isOverridden,
                        Name = name,
                        ChineseName = chineseName,
                        Labels = labelsCsv.ToListWithTrim(), // same behavior as before
                        Description = description,
                        Reference = reference,
                        Intelligence = intelligence,
                        ChineseDescription = chineseDescription,
                        // Optional: if your Threat entity has these properties:
                        // CreatedDate      = File.GetCreationTimeUtc(file),
                        // LastUpdated      = File.GetLastWriteTimeUtc(file)
                    };

                    threats.Add(threat);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse threat from YAML file: {File}", file);
                }
            }

            return threats;
        }

    }
}

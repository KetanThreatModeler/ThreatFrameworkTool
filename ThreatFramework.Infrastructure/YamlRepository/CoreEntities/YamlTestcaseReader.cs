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
    public class YamlTestcaseReader : YamlReaderBase, IYamlTestcaseReader
    {
        private readonly ILogger<YamlTestcaseReader>? _logger;

        public YamlTestcaseReader(ILogger<YamlTestcaseReader>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Parse a set of YAML files into TestCase entities.
        /// Only files with kind: test-case are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<TestCase>> GetTestCasesFromFilesAsync(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null) throw new ArgumentNullException(nameof(yamlFilePaths));

            var testCases = new List<TestCase>();

            // We iterate sequentially (like the other reader) to keep logging + error flow simple and predictable.
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
                    if (!string.Equals(kind, "test-case", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogDebug("Skipping non-test-case YAML (kind: {Kind}) in {File}", kind ?? "<null>", file);
                        continue;
                    }

                    // Root + spec
                    if (!TryLoadSpec(yaml, out var spec, out var root))
                    {
                        _logger?.LogWarning("Missing 'spec' node. Skipping: {File}", file);
                        continue;
                    }

                    // metadata
                    if (!TryGetMap(root, "metadata", out var metadata))
                    {
                        _logger?.LogWarning("Missing 'metadata' node. Skipping: {File}", file);
                        continue;
                    }

                    // Required metadata scalars
                    var guidStr = RequiredScalar(metadata, "guid", file);
                    var name = RequiredScalar(metadata, "name", file);
                    var libraryGuidStr = RequiredScalar(metadata, "libraryGuid", file);

                    // metadata.labels: sequence -> comma-separated string (null if empty)
                    string? labels = null;
                    if (metadata.Children.TryGetValue(new YamlScalarNode("labels"), out var labelsNode)
                        && labelsNode is YamlSequenceNode seq && seq.Children.Count > 0)
                    {
                        var labelValues = seq.Children
                            .OfType<YamlScalarNode>()
                            .Where(s => !string.IsNullOrWhiteSpace(s.Value))
                            .Select(s => s.Value!.Trim());
                        var joined = string.Join(",", labelValues);
                        labels = string.IsNullOrWhiteSpace(joined) ? null : joined;
                    }

                    // spec fields
                    TryGetScalar(spec, "description", out var descriptionRaw);
                    var description = string.IsNullOrWhiteSpace(descriptionRaw)
                        ? null
                        : System.Net.WebUtility.HtmlDecode(descriptionRaw);

                    // spec.flags
                    bool isHidden = false, isOverridden = false;
                    if (TryGetMap(spec, "flags", out var flags))
                    {
                        isHidden = GetBool(flags, "isHidden", false);
                        isOverridden = GetBool(flags, "isOverridden", false);
                    }

                    // i18n.zh for Chinese translations
                    string? chineseName = null, chineseDescription = null;
                    if (TryGetMap(spec, "i18n", out var i18n) && TryGetMap(i18n, "zh", out var zh))
                    {
                        TryGetScalar(zh, "name", out chineseName);
                        TryGetScalar(zh, "description", out var chineseDescRaw);
                        chineseDescription = string.IsNullOrWhiteSpace(chineseDescRaw)
                            ? null
                            : System.Net.WebUtility.HtmlDecode(chineseDescRaw);
                    }

                    var testCase = new TestCase
                    {
                        Id = 0, // Typically set by persistence layer
                        Guid = G(guidStr, "metadata.guid", file),
                        LibraryId = G(libraryGuidStr, "metadata.libraryGuid", file),
                        IsHidden = isHidden,
                        IsOverridden = isOverridden,
                        CreatedDate = File.GetCreationTimeUtc(file),
                        LastUpdated = File.GetLastWriteTimeUtc(file),
                        Name = name,
                        ChineseName = string.IsNullOrWhiteSpace(chineseName) ? null : chineseName,
                        Labels = labels,
                        Description = description,
                        ChineseDescription = chineseDescription
                    };

                    testCases.Add(testCase);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse test case from YAML file: {File}", file);
                }
            }

            return testCases;
        }
    }
}

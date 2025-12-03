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

                    // Required top-level scalars (from your sample YAML)
                    // kind: test-case
                    // apiVersion: v1
                    // guid: "..."
                    // name: "..."
                    // libraryGuid: "..."
                    var guidStr = RequiredScalar(root, "guid", file);
                    var name = RequiredScalar(root, "name", file);
                    var libraryGuidStr = RequiredScalar(root, "libraryGuid", file);

                    // labels: []  (sequence) -> comma-separated string or null
                    string? labels = null;
                    if (root.Children.TryGetValue(new YamlScalarNode("labels"), out var labelsNode)
                        && labelsNode is YamlSequenceNode labelsSeq
                        && labelsSeq.Children.Count > 0)
                    {
                        var labelValues = labelsSeq.Children
                            .OfType<YamlScalarNode>()
                            .Where(s => !string.IsNullOrWhiteSpace(s.Value))
                            .Select(s => s.Value!.Trim());

                        var joined = string.Join(",", labelValues);
                        labels = string.IsNullOrWhiteSpace(joined) ? null : joined;
                    }

                    // description: "..."
                    TryGetScalar(root, "description", out var descriptionRaw);
                    var description = string.IsNullOrWhiteSpace(descriptionRaw)
                        ? null
                        : System.Net.WebUtility.HtmlDecode(descriptionRaw);

                    // chineseName: ""
                    TryGetScalar(root, "chineseName", out var chineseNameRaw);
                    string? chineseName = string.IsNullOrWhiteSpace(chineseNameRaw) ? null : chineseNameRaw;

                    // chineseDescription: ""
                    TryGetScalar(root, "chineseDescription", out var chineseDescRaw);
                    string? chineseDescription = string.IsNullOrWhiteSpace(chineseDescRaw)
                        ? null
                        : System.Net.WebUtility.HtmlDecode(chineseDescRaw);

                    // flags:
                    //   isHidden: false
                    //   isOverridden: false
                    bool isHidden = false, isOverridden = false;
                    if (TryGetMap(root, "flags", out var flags))
                    {
                        isHidden = GetBool(flags, "isHidden", false);
                        isOverridden = GetBool(flags, "isOverridden", false);
                    }

                    var testCase = new TestCase
                    {
                        Id = 0, // set by persistence later
                        Guid = G(guidStr, "guid", file),
                        LibraryId = G(libraryGuidStr, "libraryGuid", file), // Guid of library
                        IsHidden = isHidden,
                        IsOverridden = isOverridden,
                        Name = name,
                        ChineseName = chineseName,
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

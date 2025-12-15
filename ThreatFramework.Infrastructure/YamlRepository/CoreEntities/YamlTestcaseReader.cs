using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using ThreatModeler.TF.Core.Model.CoreEntities;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository.CoreEntities
{
    public class YamlTestcaseReader : YamlReaderBase, IYamlTestcaseReader
    {
        private readonly ILogger<YamlTestcaseReader> _logger;

        private const string EntityDisplayName = "TestCase";

        public YamlTestcaseReader(ILogger<YamlTestcaseReader>? logger = null)
        {
            _logger = logger ?? NullLogger<YamlTestcaseReader>.Instance;
        }

        public Task<TestCase> GetTestCaseFromFileAsync(string yamlFilePath)
            => LoadYamlEntityAsync(
                yamlFilePath,
                _logger,
                ParseTestCase,
                EntityDisplayName,
                cancellationToken: default);

        /// <summary>
        /// Parse a set of YAML files into TestCase entities.
        /// Only files with kind: test-case are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<TestCase>> GetTestCasesFromFilesAsync(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null)
            {
                throw new ArgumentNullException(nameof(yamlFilePaths));
            }

            var testCases = new List<TestCase>();

            foreach (var file in yamlFilePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    // DRY: use shared IO helper instead of File.Exists + File.ReadAllTextAsync
                    var yaml = await TryReadYamlFileAsync(file, _logger, cancellationToken: default);
                    if (yaml is null)
                    {
                        // read error already logged by TryReadYamlFileAsync
                        continue;
                    }

                    var testCase = ParseTestCase(yaml, file);
                    if (testCase is not null)
                    {
                        testCases.Add(testCase);
                    }
                    // If null, ParseTestCase already logged why it was skipped.
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse {Entity} from YAML file: {File}", EntityDisplayName, file);
                }
            }

            return testCases;
        }

        #region Parsing

        /// <summary>
        /// Parses a single TestCase from YAML content.
        /// Returns null if the document is not a 'test-case' kind or is malformed.
        /// For single-file API, LoadYamlEntityAsync will treat null as an error and throw.
        /// </summary>
        private TestCase? ParseTestCase(string yaml, string filePath)
        {
            try
            {
                // Validate kind
                var kind = ReadKind(yaml);
                if (!string.Equals(kind, "test-case", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug(
                        "Skipping non-test-case YAML (kind: {Kind}) in {File}",
                        kind ?? "<null>",
                        filePath);

                    return null;
                }

                // Root is the spec for TestCase YAML
                if (!TryLoadRoot(yaml, out var root))
                {
                    _logger.LogWarning("YAML root is not a mapping. Skipping: {File}", filePath);
                    return null;
                }

                // Required top-level scalars
                var guidStr = RequiredScalar(root, "guid", filePath);
                var name = RequiredScalar(root, "name", filePath);
                var libraryGuidStr = RequiredScalar(root, "libraryGuid", filePath);

                // labels: sequence -> comma-separated string or null
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

                // description
                TryGetScalar(root, "description", out var descriptionRaw);
                var description = string.IsNullOrWhiteSpace(descriptionRaw)
                    ? null
                    : System.Net.WebUtility.HtmlDecode(descriptionRaw);

                // chineseName
                TryGetScalar(root, "chineseName", out var chineseNameRaw);
                var chineseName = string.IsNullOrWhiteSpace(chineseNameRaw)
                    ? null
                    : chineseNameRaw;

                // chineseDescription
                TryGetScalar(root, "chineseDescription", out var chineseDescRaw);
                var chineseDescription = string.IsNullOrWhiteSpace(chineseDescRaw)
                    ? null
                    : System.Net.WebUtility.HtmlDecode(chineseDescRaw);

                // flags:
                //   isHidden: false
                //   isOverridden: false
                var isHidden = false;
                var isOverridden = false;
                if (TryGetMap(root, "flags", out var flags))
                {
                    isHidden = GetBool(flags, "isHidden", false);
                    isOverridden = GetBool(flags, "isOverridden", false);
                }

                var testCase = new TestCase
                {
                    Guid = G(guidStr, "guid", filePath),
                    LibraryId = G(libraryGuidStr, "libraryGuid", filePath),
                    IsHidden = isHidden,
                    IsOverridden = isOverridden,
                    Name = name,
                    ChineseName = chineseName,
                    Labels = labels,
                    Description = description,
                    ChineseDescription = chineseDescription
                };

                return testCase;
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

using Microsoft.Extensions.Logging;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Implmentation.Helper;
using YamlDotNet.RepresentationModel;

namespace ThreatFramework.Infrastructure.YamlRepository.CoreEntities
{
    public class YamlLibraryReader : YamlReaderBase, IYamlLibraryReader
    {
        private readonly ILogger<YamlLibraryReader>? _logger;

        public YamlLibraryReader(ILogger<YamlLibraryReader>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Parse a set of YAML files into Library entities.
        /// Only files with kind: library are considered; others are ignored.
        /// </summary>
        public async Task<IEnumerable<Library>> GetLibrariesFromFilesAsync(IEnumerable<string> yamlFilePaths)
        {
            if (yamlFilePaths is null) throw new ArgumentNullException(nameof(yamlFilePaths));

            var libraries = new List<Library>();

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
                    if (!string.Equals(kind, "library", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogDebug("Skipping non-library YAML (kind: {Kind}) in {File}", kind ?? "<null>", file);
                        continue;
                    }

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

                    // Required fields
                    var guidStr = RequiredScalar(root, "guid", file);
                    var name = RequiredScalar(root, "name", file);

                    // Optional scalars (default to empty string)
                    TryGetScalar(root, "version", out var version);
                    TryGetScalar(root, "description", out var descriptionRaw);
                    TryGetScalar(root, "imageUrl", out var imageUrl);

                    // New fields from your sample YAML
                    TryGetScalar(root, "releaseNote", out var releaseNote);
                    TryGetScalar(root, "sharingType", out var sharingType);

                    // Clean/normalize
                    var description = string.IsNullOrWhiteSpace(descriptionRaw)
                        ? string.Empty
                        : System.Net.WebUtility.HtmlDecode(descriptionRaw);

                    version = string.IsNullOrWhiteSpace(version) ? string.Empty : version.Trim();
                    imageUrl = string.IsNullOrWhiteSpace(imageUrl) ? string.Empty : imageUrl.Trim();
                    releaseNote = string.IsNullOrWhiteSpace(releaseNote) ? string.Empty : releaseNote.Trim();
                    sharingType = string.IsNullOrWhiteSpace(sharingType) ? string.Empty : sharingType.Trim();

                    // labels: [] -> comma-separated string (empty string if none)
                    var labels = string.Empty;
                    if (root.Children.TryGetValue(new YamlScalarNode("labels"), out var labelsNode)
                        && labelsNode is YamlSequenceNode seq && seq.Children.Count > 0)
                    {
                        var labelValues = seq.Children
                            .OfType<YamlScalarNode>()
                            .Where(s => !string.IsNullOrWhiteSpace(s.Value))
                            .Select(s => s.Value!.Trim());

                        labels = string.Join(",", labelValues);
                    }

                    // flags at root
                    bool readonlyFlag = GetBool(root, "readonly", false);
                    bool isDefault = GetBool(root, "isDefault", false);

                    var library = new Library
                    {
                        Guid = G(guidStr, "guid", file),
                        DepartmentId = 0, // Not present in YAML; set elsewhere if needed
                        Readonly = readonlyFlag,
                        IsDefault = isDefault,

                        Name = name,
                        SharingType = sharingType,          // ✅ new
                        Description = description,
                        Labels = labels.ToLabelList(),
                        Version = version,
                        ReleaseNotes = releaseNote,         // ✅ YAML: releaseNote -> Library.ReleaseNotes
                        ImageURL = imageUrl
                    };

                    libraries.Add(library);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse library from YAML file: {File}", file);
                }
            }

            return libraries;
        }

    }
}

using Microsoft.Extensions.Logging;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract.YamlRepository.CoreEntity;
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
                    if (!string.Equals(kind, "library", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogDebug("Skipping non-library YAML (kind: {Kind}) in {File}", kind ?? "<null>", file);
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

                    // Optional metadata scalars
                    TryGetScalar(metadata, "version", out var version);

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

                    TryGetScalar(spec, "imageUrl", out var imageUrl);
                    TryGetScalar(spec, "createdAt", out var createdAtStr);
                    TryGetScalar(spec, "updatedAt", out var updatedAtStr);

                    // spec flags
                    bool readonlyFlag = GetBool(spec, "readonly", false);
                    bool isDefault = GetBool(spec, "isDefault", false);

                    // Parse dates
                    var dateCreated = DateTime.UtcNow;
                    var lastUpdated = DateTime.UtcNow;

                    if (!string.IsNullOrWhiteSpace(createdAtStr) && DateTime.TryParse(createdAtStr, out var parsedCreated))
                    {
                        dateCreated = parsedCreated.ToUniversalTime();
                    }
                    else
                    {
                        dateCreated = File.GetCreationTimeUtc(file);
                    }

                    if (!string.IsNullOrWhiteSpace(updatedAtStr) && DateTime.TryParse(updatedAtStr, out var parsedUpdated))
                    {
                        lastUpdated = parsedUpdated.ToUniversalTime();
                    }
                    else
                    {
                        lastUpdated = File.GetLastWriteTimeUtc(file);
                    }

                    var library = new Library
                    {
                        Id = 0, // Typically set by persistence layer
                        Guid = G(guidStr, "metadata.guid", file),
                        DepartmentId = 0, // Not present in YAML; set elsewhere if needed
                        DateCreated = dateCreated,
                        LastUpdated = lastUpdated,
                        Readonly = readonlyFlag,
                        IsDefault = isDefault,
                        Name = name,
                        SharingType = null, // Not present in your YAML shape
                        Description = description,
                        Labels = labels,
                        Version = string.IsNullOrWhiteSpace(version) ? null : version,
                        ReleaseNotes = null, // Not present in your YAML shape
                        ImageURL = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl
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

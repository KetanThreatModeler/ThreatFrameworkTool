using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.YamlRepository;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ThreatFramework.Infrastructure.YamlRepository
{
    public abstract class YamlReaderBase
    {
        protected readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        #region File enumeration / low-level YAML

        protected static IEnumerable<string> EnumerateYamlFiles(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException(folderPath);
            }

            return Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(p =>
                    p.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                    p.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));
        }

        protected static string? ReadKind(string yaml)
        {
            try
            {
                var stream = new YamlStream();
                using var sr = new StringReader(yaml);
                stream.Load(sr);

                if (stream.Documents.Count == 0) return null;
                if (stream.Documents[0].RootNode is not YamlMappingNode root) return null;

                var key = new YamlScalarNode("kind");
                return root.Children.TryGetValue(key, out var v) && v is YamlScalarNode s
                    ? s.Value
                    : null;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Scalar / mapping helpers

        protected static Guid G(string? raw, string field, string file)
        {
            if (Guid.TryParse(raw, out var g)) return g;

            throw new InvalidOperationException(
                $"Invalid or missing GUID for '{field}' in '{file}' (value: '{raw ?? "<null>"}').");
        }

        protected static bool TryLoadRoot(string yaml, out YamlMappingNode root)
        {
            root = default!;
            try
            {
                var stream = new YamlStream();
                using var sr = new StringReader(yaml);
                stream.Load(sr);

                if (stream.Documents.Count == 0) return false;
                if (stream.Documents[0].RootNode is not YamlMappingNode r) return false;

                root = r;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Parse YAML and return the "spec" mapping (and root) if present.</summary>
        protected static bool TryLoadSpec(string yaml, out YamlMappingNode spec, out YamlMappingNode root)
        {
            spec = default!;
            if (!TryLoadRoot(yaml, out root)) return false;

            return TryGetMap(root, "spec", out spec);
        }

        protected static bool TryGetMap(YamlMappingNode map, string key, out YamlMappingNode child)
        {
            var k = new YamlScalarNode(key);
            if (map.Children.TryGetValue(k, out var node) && node is YamlMappingNode m)
            {
                child = m;
                return true;
            }

            child = default!;
            return false;
        }

        protected static bool TryGetScalar(YamlMappingNode map, string key, out string value)
        {
            value = string.Empty;
            var k = new YamlScalarNode(key);

            if (map.Children.TryGetValue(k, out var node) &&
                node is YamlScalarNode s &&
                s.Value is not null)
            {
                value = s.Value;
                return true;
            }

            return false;
        }

        protected static string RequiredScalar(YamlMappingNode map, string key, string file)
        {
            if (TryGetScalar(map, key, out var v) && !string.IsNullOrWhiteSpace(v)) return v;

            throw new InvalidOperationException($"Missing required field '{key}' in '{file}'.");
        }

        protected static bool GetBool(YamlMappingNode map, string key, bool defaultValue = false)
            => TryGetScalar(map, key, out var raw) && bool.TryParse(raw, out var b) ? b : defaultValue;

        #endregion

        #region Shared IO / validation helpers

        /// <summary>
        /// Validates that a YAML file path is non-empty and exists. Throws on failure.
        /// </summary>
        protected static void ValidateYamlFilePath(
            string yamlFilePath,
            ILogger logger,
            string entityDisplayName)
        {
            if (string.IsNullOrWhiteSpace(yamlFilePath))
            {
                logger.LogError("YAML file path for {Entity} is null or whitespace.", entityDisplayName);
                throw new ArgumentException("YAML file path cannot be null or whitespace.", nameof(yamlFilePath));
            }

            if (!File.Exists(yamlFilePath))
            {
                logger.LogError(
                    "YAML file for {Entity} not found: {YamlFilePath}",
                    entityDisplayName,
                    yamlFilePath);

                throw new FileNotFoundException($"YAML file '{yamlFilePath}' does not exist.", yamlFilePath);
            }
        }

        /// <summary>
        /// Builds the full YAML folder path for a given entity type and ensures it exists.
        /// </summary>
        protected static string BuildAndValidateYamlFolder(
            string rootFolderPath,
            string entitySubFolderName,
            ILogger logger,
            string entityDisplayName)
        {
            if (string.IsNullOrWhiteSpace(rootFolderPath))
            {
                logger.LogError(
                    "Root folder path for YAML {Entity} is null or whitespace.",
                    entityDisplayName);

                throw new ArgumentException("Folder path cannot be null or whitespace.", nameof(rootFolderPath));
            }

            var fullPath = Path.Combine(
                rootFolderPath,
                YamlFolderConstants.MappingsRootFolder,
                entitySubFolderName);

            if (!Directory.Exists(fullPath))
            {
                logger.LogError(
                    "YAML folder for {Entity} not found: {MappingFolder}",
                    entityDisplayName,
                    fullPath);

                throw new DirectoryNotFoundException($"Folder {fullPath} does not exist");
            }

            return fullPath;
        }

        protected static async Task<string?> TryReadYamlFileAsync(
            string filePath,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                return await File.ReadAllTextAsync(filePath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Reading YAML file was cancelled: {FilePath}", filePath);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read YAML file: {FilePath}", filePath);
                return null;
            }
        }

        protected static async Task<string> ReadYamlContentOrThrowAsync(
            string filePath,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var yaml = await TryReadYamlFileAsync(filePath, logger, cancellationToken);

            if (yaml is null)
            {
                logger.LogError("Failed to read YAML file: {YamlFilePath}", filePath);
                throw new InvalidOperationException($"Failed to read YAML file '{filePath}'.");
            }

            return yaml;
        }

        #endregion

        #region High-level entity loaders (generic, DRY)

        /// <summary>
        /// Loads a single YAML-based entity from a file path using a parse delegate.
        /// Handles validation, IO, logging, and error reporting.
        /// </summary>
        protected async Task<T> LoadYamlEntityAsync<T>(
            string yamlFilePath,
            ILogger logger,
            Func<string, string, T?> parseEntity,
            string entityDisplayName,
            CancellationToken cancellationToken)
        {
            ValidateYamlFilePath(yamlFilePath, logger, entityDisplayName);

            var yaml = await ReadYamlContentOrThrowAsync(yamlFilePath, logger, cancellationToken);

            var entity = parseEntity(yaml, yamlFilePath);
            if (entity is null)
            {
                logger.LogError(
                    "Failed to parse {Entity} from YAML file: {YamlFilePath}",
                    entityDisplayName,
                    yamlFilePath);

                throw new InvalidOperationException(
                    $"Failed to parse {entityDisplayName} from YAML file '{yamlFilePath}'.");
            }

            return entity;
        }

        /// <summary>
        /// Loads all YAML-based entities of a given type from a folder.
        /// Handles folder resolution, enumeration, IO, and partial-failure logging.
        /// </summary>
        protected async Task<List<T>> LoadYamlEntitiesFromFolderAsync<T>(
            string rootFolderPath,
            string entitySubFolderName,
            ILogger logger,
            Func<string, string, T?> parseEntity,
            string entityDisplayName,
            CancellationToken cancellationToken)
        {
            var folderPath = BuildAndValidateYamlFolder(
                rootFolderPath,
                entitySubFolderName,
                logger,
                entityDisplayName);

            var results = new List<T>();

            foreach (var file in EnumerateYamlFiles(folderPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var yaml = await TryReadYamlFileAsync(file, logger, cancellationToken);
                if (yaml is null)
                {
                    // Warning already logged
                    continue;
                }

                var entity = parseEntity(yaml, file);
                if (entity is not null)
                {
                    results.Add(entity);
                }
            }

            return results;
        }

        protected string SanitizeInvalidYamlEscapes(string yaml)
        {
            var sb = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < yaml.Length; i++)
            {
                char c = yaml[i];

                if (c == '"' && (i == 0 || yaml[i - 1] != '\\'))
                {
                    insideQuotes = !insideQuotes;
                    sb.Append(c);
                    continue;
                }

                if (insideQuotes && c == '\\')
                {
                    // Escape only if NOT already escaped
                    if (i + 1 < yaml.Length && yaml[i + 1] != '\\')
                    {
                        sb.Append('\\'); // add extra backslash
                    }
                }

                sb.Append(c);
            }

            return sb.ToString();
        }


        #endregion
    }
}

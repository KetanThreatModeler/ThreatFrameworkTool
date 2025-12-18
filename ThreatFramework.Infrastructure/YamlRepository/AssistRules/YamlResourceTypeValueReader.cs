using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ThreatFramework.Infrastructure.YamlRepository;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Infra.Contract.YamlRepository.AssistRules;

namespace ThreatModeler.TF.Infra.Implmentation.YamlRepository.AssistRules
{
    public sealed class YamlResourceTypeValueReader : YamlReaderBase, IYamlResourceTypesValueReader
    {
        private readonly ILogger<YamlResourceTypeValueReader> _logger;
        private const string EntityDisplayName = "ResourceTypeValues";

        public YamlResourceTypeValueReader(ILogger<YamlResourceTypeValueReader>? logger = null)
        {
            _logger = logger ?? NullLogger<YamlResourceTypeValueReader>.Instance;
        }

        public Task<ResourceTypeValues> GetResourceTypeValue(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("ResourceTypeValues YAML file path is null or empty.", nameof(path));

            return LoadYamlEntityAsync(
                path,
                _logger,
                ParseStrict,
                EntityDisplayName,
                cancellationToken: CancellationToken.None);
        }

        public async Task<IEnumerable<ResourceTypeValues>> GetResourceTypeValues(IEnumerable<string> paths)
        {
            if (paths is null)
                throw new ArgumentNullException(nameof(paths));

            var pathList = paths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (pathList.Count == 0)
                throw new ArgumentException("No valid ResourceTypeValues YAML file paths provided.", nameof(paths));

            var result = new List<ResourceTypeValues>();

            foreach (var file in pathList)
            {
                _logger.LogDebug("Reading ResourceTypeValues YAML file: {File}", file);

                var yaml = await TryReadYamlFileAsync(file, _logger, CancellationToken.None)
                           ?? throw new InvalidOperationException($"Failed to read YAML file: {file}");

                var entity = ParseStrict(yaml, file)
                    ?? throw new InvalidOperationException($"Failed to parse ResourceTypeValues YAML file: {file}");

                result.Add(entity);
            }

            return result;
        }

        // -------------------------
        // STRICT Parsing (no kind validation)
        // -------------------------
        private ResourceTypeValues ParseStrict(string yaml, string filePath)
        {
            if (!TryLoadRoot(yaml, out var root))
                throw new InvalidOperationException($"YAML root is not a mapping. File: {filePath}");

            // Required fields
            var resourceName = RequiredScalar(root, "resourceName", filePath);
            var resourceTypeValue = RequiredScalar(root, "resourceTypeValue", filePath);
            var componentGuidStr = RequiredScalar(root, "componentGuid", filePath);
            var libraryGuidStr = RequiredScalar(root, "libraryGuid", filePath);

            var entity = new ResourceTypeValues
            {
                ResourceName = resourceName,
                ResourceTypeValue = resourceTypeValue,
                ComponentGuid = G(componentGuidStr, "componentGuid", filePath),
                LibraryId = G(libraryGuidStr, "libraryGuid", filePath) // model property name is LibraryId but it's a Guid
            };

            _logger.LogDebug(
                "Parsed ResourceTypeValues YAML successfully. ResourceTypeValue={RTV}, LibraryGuid={Lib}, ComponentGuid={Comp}",
                entity.ResourceTypeValue,
                entity.LibraryId,
                entity.ComponentGuid);

            return entity;
        }
    }
}

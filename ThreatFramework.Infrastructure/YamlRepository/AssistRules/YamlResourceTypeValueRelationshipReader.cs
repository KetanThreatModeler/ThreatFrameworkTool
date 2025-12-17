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
    public sealed class YamlResourceTypeValueRelationshipReader : YamlReaderBase, IYamlResourceTypesValueRelationshipReader
    {
        private readonly ILogger<YamlResourceTypeValueRelationshipReader> _logger;
        private const string EntityDisplayName = "ResourceTypeValueRelationship";

        public YamlResourceTypeValueRelationshipReader(ILogger<YamlResourceTypeValueRelationshipReader>? logger = null)
        {
            _logger = logger ?? NullLogger<YamlResourceTypeValueRelationshipReader>.Instance;
        }

        public Task<ResourceTypeValueRelationship> GetResourceTypeValueRelationship(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("ResourceTypeValueRelationship YAML file path is null or empty.", nameof(filePath));

            return LoadYamlEntityAsync(
                filePath,
                _logger,
                ParseStrict,
                EntityDisplayName,
                cancellationToken: CancellationToken.None);
        }

        public async Task<IEnumerable<ResourceTypeValueRelationship>> GetResourceTypeValueRelationships(IEnumerable<string> filePaths)
        {
            if (filePaths is null)
                throw new ArgumentNullException(nameof(filePaths));

            var pathList = filePaths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (pathList.Count == 0)
                throw new ArgumentException("No valid ResourceTypeValueRelationship YAML file paths provided.", nameof(filePaths));

            var result = new List<ResourceTypeValueRelationship>();

            foreach (var file in pathList)
            {
                _logger.LogDebug("Reading ResourceTypeValueRelationship YAML file: {File}", file);

                var yaml = await TryReadYamlFileAsync(file, _logger, CancellationToken.None)
                           ?? throw new InvalidOperationException($"Failed to read YAML file: {file}");

                var entity = ParseStrict(yaml, file)
                    ?? throw new InvalidOperationException($"Failed to parse ResourceTypeValueRelationship YAML file: {file}");

                result.Add(entity);
            }

            return result;
        }

        // -------------------------
        // STRICT Parsing (no kind validation)
        // -------------------------
        private ResourceTypeValueRelationship ParseStrict(string yaml, string filePath)
        {
            if (!TryLoadRoot(yaml, out var root))
                throw new InvalidOperationException($"YAML root is not a mapping. File: {filePath}");

            // Required top-level fields
            var source = RequiredScalar(root, "sourceResourceTypeValue", filePath);
            var target = RequiredScalar(root, "targetResourceTypeValue", filePath);
            var relationshipGuidStr = RequiredScalar(root, "relationshipGuid", filePath);
            var libraryGuidStr = RequiredScalar(root, "libraryGuid", filePath);

            // Optional flags (default false if flags not present)
            var isRequired = false;
            var isDeleted = false;

            if (TryGetMap(root, "flags", out var flags))
            {
                isRequired = GetBool(flags, "isRequired", defaultValue: false);
                isDeleted = GetBool(flags, "isDeleted", defaultValue: false);
            }

            var entity = new ResourceTypeValueRelationship
            {
                SourceResourceTypeValue = source,
                TargetResourceTypeValue = target,
                RelationshipGuid = G(relationshipGuidStr, "relationshipGuid", filePath),
                LibraryId = G(libraryGuidStr, "libraryGuid", filePath),
                IsRequired = isRequired,
                IsDeleted = isDeleted
            };

            _logger.LogDebug(
                "Parsed ResourceTypeValueRelationship YAML successfully. Source={Source}, RelGuid={RelGuid}, Target={Target}, Lib={Lib}",
                entity.SourceResourceTypeValue,
                entity.RelationshipGuid,
                entity.TargetResourceTypeValue,
                entity.LibraryId);

            return entity;
        }
    }
}

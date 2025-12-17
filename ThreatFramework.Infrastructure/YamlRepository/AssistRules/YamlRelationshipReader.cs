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
    public sealed class YamlRelationshipReader : YamlReaderBase, IYamlRelationshipReader
    {
        private readonly ILogger<YamlRelationshipReader> _logger;
        private const string EntityDisplayName = "Relationship";

        public YamlRelationshipReader(ILogger<YamlRelationshipReader>? logger = null)
        {
            _logger = logger ?? NullLogger<YamlRelationshipReader>.Instance;
        }

        public Task<Relationship> ReadRelationshipAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Relationship YAML file path is null or empty.", nameof(filePath));

            return LoadYamlEntityAsync(
                filePath,
                _logger,
                ParseRelationshipStrict,
                EntityDisplayName,
                cancellationToken: CancellationToken.None);
        }

        public async Task<IReadOnlyList<Relationship>> ReadRelationshipsAsync(IEnumerable<string> filePaths)
        {
            if (filePaths is null)
                throw new ArgumentNullException(nameof(filePaths));

            var paths = filePaths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (paths.Count == 0)
                throw new ArgumentException("No valid Relationship YAML file paths provided.", nameof(filePaths));

            var result = new List<Relationship>();

            foreach (var file in paths)
            {
                _logger.LogDebug("Reading Relationship YAML file: {File}", file);

                // Will throw if file not found or unreadable
                var yaml = await TryReadYamlFileAsync(file, _logger, CancellationToken.None)
                           ?? throw new InvalidOperationException($"Failed to read YAML file: {file}");

                var relationship = ParseRelationshipStrict(yaml, file)
                    ?? throw new InvalidOperationException($"Failed to parse Relationship YAML file: {file}");

                result.Add(relationship);
            }

            return result;
        }

        // -------------------------
        // STRICT Parsing (no kind validation)
        // -------------------------
        private Relationship ParseRelationshipStrict(string yaml, string filePath)
        {
            if (!TryLoadRoot(yaml, out var root))
            {
                throw new InvalidOperationException(
                    $"YAML root is not a mapping. File: {filePath}");
            }

            // Required fields (will throw if missing)
            var guidStr = RequiredScalar(root, "relationshipGuid", filePath);
            var relationshipName = RequiredScalar(root, "relationshipName", filePath);

            // Optional fields
            TryGetScalar(root, "description", out var descriptionRaw);
            TryGetScalar(root, "chineseRelationship", out var chineseRaw);

            var relationship = new Relationship
            {
                Guid = G(guidStr, "relationshipGuid", filePath),
                RelationshipName = relationshipName,
                Description = descriptionRaw ?? string.Empty,
                ChineseRelationship = chineseRaw ?? string.Empty
            };

            _logger.LogDebug(
                "Parsed Relationship YAML successfully. Guid={Guid}, Name={Name}",
                relationship.Guid,
                relationship.RelationshipName);

            return relationship;
        }
    }
}

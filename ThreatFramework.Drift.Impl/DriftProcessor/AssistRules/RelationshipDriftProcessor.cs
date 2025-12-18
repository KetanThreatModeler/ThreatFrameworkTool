using Microsoft.Extensions.Logging;
using ThreatFramework.Core;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor.AssistRules
{
    public static class RelationshipDriftProcessor
    {
        public static async Task ProcessAsync(
            TMFrameworkDriftDto drift,
            EntityFileChangeSet relationshipChanges,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (relationshipChanges == null) throw new ArgumentNullException(nameof(relationshipChanges));
            if (yamlReader == null) throw new ArgumentNullException(nameof(yamlReader));
            if (driftOptions == null) throw new ArgumentNullException(nameof(driftOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            await ProcessAddedAsync(drift, relationshipChanges.AddedFilePaths, yamlReader, logger);
            await ProcessDeletedAsync(drift, relationshipChanges.DeletedFilePaths, yamlReader, logger);
            await ProcessModifiedAsync(drift, relationshipChanges.ModifiedFiles, yamlReader, driftOptions, logger);
        }

        // ---------------------------
        // ADDED
        // ---------------------------
        private static async Task ProcessAddedAsync(
            TMFrameworkDriftDto drift,
            IEnumerable<string> addedPaths,
            IYamlReaderRouter yamlReader,
            ILogger logger)
        {
            var pathList = NormalizePathList(addedPaths);
            if (pathList.Count == 0)
            {
                logger.LogInformation("No added Relationship files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} added Relationship files...", pathList.Count);

            var relationships = await ReadRelationshipsAsync(yamlReader, pathList, logger, "added");

            foreach (var relationship in relationships)
            {
                if (relationship == null)
                {
                    logger.LogWarning("Encountered null Relationship while processing added Relationships.");
                    continue;
                }

                drift.Global.Relatioships.Added.Add(relationship);

                logger.LogInformation(
                    "Added Relationship {Guid} ({Name}) attached to TMFrameworkDrift.Global.Relatioships.Added.",
                    relationship.Guid,
                    relationship.RelationshipName);
            }
        }

        // ---------------------------
        // DELETED
        // ---------------------------
        private static async Task ProcessDeletedAsync(
            TMFrameworkDriftDto drift,
            IEnumerable<string> deletedPaths,
            IYamlReaderRouter yamlReader,
            ILogger logger)
        {
            var pathList = NormalizePathList(deletedPaths);
            if (pathList.Count == 0)
            {
                logger.LogInformation("No deleted Relationship files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} deleted Relationship files...", pathList.Count);

            var relationships = await ReadRelationshipsAsync(yamlReader, pathList, logger, "deleted");

            foreach (var relationship in relationships)
            {
                if (relationship == null)
                {
                    logger.LogWarning("Encountered null Relationship while processing deleted Relationships.");
                    continue;
                }

                drift.Global.Relatioships.Removed.Add(relationship);

                logger.LogInformation(
                    "Deleted Relationship {Guid} ({Name}) attached to TMFrameworkDrift.Global.Relatioships.Removed.",
                    relationship.Guid,
                    relationship.RelationshipName);
            }
        }

        // ---------------------------
        // MODIFIED
        // ---------------------------
        private static async Task ProcessModifiedAsync(
            TMFrameworkDriftDto drift,
            IEnumerable<ModifiedFilePathInfo> modifiedPaths,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (modifiedPaths == null)
            {
                logger.LogInformation("No modified Relationship files detected.");
                return;
            }

            var pathList = modifiedPaths.Where(m => m != null).ToList();
            if (pathList.Count == 0)
            {
                logger.LogInformation("No modified Relationship files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} modified Relationship files...", pathList.Count);

            foreach (var modified in pathList)
            {
                Relationship? baseRelationship;
                Relationship? targetRelationship;

                try
                {
                    baseRelationship = await ReadSingleRelationshipAsync(
                        yamlReader,
                        modified.BaseRepositoryFilePath,
                        logger);

                    targetRelationship = await ReadSingleRelationshipAsync(
                        yamlReader,
                        modified.TargetRepositoryFilePath,
                        logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error reading Relationship YAML for modified file. Base={BasePath}, Target={TargetPath}",
                        modified.BaseRepositoryFilePath,
                        modified.TargetRepositoryFilePath);
                    throw;
                }

                if (baseRelationship == null || targetRelationship == null)
                {
                    logger.LogWarning(
                        "Unable to read both base and target Relationship for modified file. Base null={BaseNull}, Target null={TargetNull}",
                        baseRelationship == null,
                        targetRelationship == null);
                    continue;
                }

                // Use configured default fields (you should add this property to EntityDriftAggregationOptions if not present)
                var changedFields = targetRelationship.CompareFields(
                    baseRelationship,
                    driftOptions.RelationshipDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation(
                        "No changes detected in configured Relationship fields for {Guid}. Skipping modification.",
                        baseRelationship.Guid);
                    continue;
                }

                var modifiedEntity = new ModifiedEntity<Relationship>
                {
                    Entity = targetRelationship,
                    ModifiedFields = changedFields,
                };

                drift.Global.Relatioships.Modified.Add(modifiedEntity);

                logger.LogInformation(
                    "Modified Relationship {Guid} ({Name}) attached to TMFrameworkDrift.Global.Relatioships.Modified.",
                    targetRelationship.Guid,
                    targetRelationship.RelationshipName);
            }
        }

        // ---------------------------
        // Helpers
        // ---------------------------
        private static List<string> NormalizePathList(IEnumerable<string> paths)
        {
            return paths?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList()
                ?? new List<string>();
        }

        private static async Task<IReadOnlyCollection<Relationship>> ReadRelationshipsAsync(
            IYamlReaderRouter yamlReader,
            IEnumerable<string> paths,
            ILogger logger,
            string operationName)
        {
            var pathList = NormalizePathList(paths);
            if (pathList.Count == 0)
                return Array.Empty<Relationship>();

            try
            {
                // Replace with your actual yamlReader method name
                var result = await yamlReader.ReadRelationshipsAsync(pathList);
                return result?.Where(r => r != null).ToList() ?? new List<Relationship>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read Relationship YAML files for operation '{Operation}'.", operationName);
                throw;
            }
        }

        private static async Task<Relationship?> ReadSingleRelationshipAsync(
            IYamlReaderRouter yamlReader,
            string path,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogWarning("ReadSingleRelationshipAsync called with an empty path.");
                return null;
            }

            try
            {
                // Replace with your actual yamlReader method name
                var list = await yamlReader.ReadRelationshipsAsync(new[] { path });
                return list?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read Relationship YAML at path: {Path}", path);
                throw;
            }
        }
    }
}

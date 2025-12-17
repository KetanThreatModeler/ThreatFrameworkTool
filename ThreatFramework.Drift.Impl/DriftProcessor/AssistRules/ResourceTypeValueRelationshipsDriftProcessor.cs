using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreatFramework.Core;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor.AssistRules
{
    public static class ResourceTypeValueRelationshipsDriftProcessor
    {
        public static async Task ProcessAsync(
            TMFrameworkDriftDto drift,
            EntityFileChangeSet resourceTypeValueRelationshipChanges,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (resourceTypeValueRelationshipChanges == null) throw new ArgumentNullException(nameof(resourceTypeValueRelationshipChanges));
            if (yamlReader == null) throw new ArgumentNullException(nameof(yamlReader));
            if (driftOptions == null) throw new ArgumentNullException(nameof(driftOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            await ProcessAddedAsync(drift, resourceTypeValueRelationshipChanges.AddedFilePaths, yamlReader, logger);
            await ProcessDeletedAsync(drift, resourceTypeValueRelationshipChanges.DeletedFilePaths, yamlReader, logger);
            await ProcessModifiedAsync(drift, resourceTypeValueRelationshipChanges.ModifiedFiles, yamlReader, driftOptions, logger);
        }

        // ─────────────────────────────────────────────────────────────
        // ADDED
        // ─────────────────────────────────────────────────────────────

        private static async Task ProcessAddedAsync(
            TMFrameworkDriftDto drift,
            IEnumerable<string> addedPaths,
            IYamlReaderRouter yamlReader,
            ILogger logger)
        {
            var pathList = NormalizePathList(addedPaths);
            if (pathList.Count == 0)
            {
                logger.LogInformation("No added ResourceTypeValueRelationship files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} added ResourceTypeValueRelationship files...", pathList.Count);

            var relationships = await ReadResourceTypeValueRelationshipsAsync(yamlReader, pathList, logger, "added");

            foreach (var rel in relationships)
            {
                if (rel == null)
                {
                    logger.LogWarning("Encountered null ResourceTypeValueRelationship while processing added ResourceTypeValueRelationship.");
                    continue;
                }

                // 1) Prefer AddedLibrary.ResourceTypeValueRelationships
                var addedLib = drift.AddedLibraries
                    .FirstOrDefault(l => l.Library != null && l.Library.Guid == rel.LibraryId);

                if (addedLib != null)
                {
                    addedLib.ResourceTypeValueRelationships.Add(rel);

                    logger.LogInformation(
                        "Added ResourceTypeValueRelationship (Src={Source}, Rel={RelGuid}, Tgt={Target}) attached to AddedLibrary {LibraryGuid}.",
                        rel.SourceResourceTypeValue,
                        rel.RelationshipGuid,
                        rel.TargetResourceTypeValue,
                        rel.LibraryId);

                    continue;
                }

                // 2) Otherwise attach under ModifiedLibraries[n].ResourceTypeValueRelationships.Added
                var libDrift = GetOrCreateLibraryDrift(drift, rel.LibraryId, logger);
                libDrift.ResourceTypeValueRelationships.Added.Add(rel);

                logger.LogInformation(
                    "Added ResourceTypeValueRelationship (Src={Source}, Rel={RelGuid}, Tgt={Target}) attached to LibraryDrift.ResourceTypeValueRelationships.Added for Library {LibraryGuid}.",
                    rel.SourceResourceTypeValue,
                    rel.RelationshipGuid,
                    rel.TargetResourceTypeValue,
                    rel.LibraryId);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // DELETED
        // ─────────────────────────────────────────────────────────────

        private static async Task ProcessDeletedAsync(
            TMFrameworkDriftDto drift,
            IEnumerable<string> deletedPaths,
            IYamlReaderRouter yamlReader,
            ILogger logger)
        {
            var pathList = NormalizePathList(deletedPaths);
            if (pathList.Count == 0)
            {
                logger.LogInformation("No deleted ResourceTypeValueRelationship files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} deleted ResourceTypeValueRelationship files...", pathList.Count);

            var relationships = await ReadResourceTypeValueRelationshipsAsync(yamlReader, pathList, logger, "deleted");

            foreach (var rel in relationships)
            {
                if (rel == null)
                {
                    logger.LogWarning("Encountered null ResourceTypeValueRelationship while processing deleted ResourceTypeValueRelationship.");
                    continue;
                }

                // 1) Prefer DeletedLibrary.ResourceTypeValueRelationships
                var deletedLib = drift.DeletedLibraries
                    .FirstOrDefault(l => l.LibraryGuid == rel.LibraryId);

                if (deletedLib != null)
                {
                    deletedLib.ResourceTypeValueRelationships.Add(rel);

                    logger.LogInformation(
                        "Deleted ResourceTypeValueRelationship (Src={Source}, Rel={RelGuid}, Tgt={Target}) attached to DeletedLibrary {LibraryGuid}.",
                        rel.SourceResourceTypeValue,
                        rel.RelationshipGuid,
                        rel.TargetResourceTypeValue,
                        rel.LibraryId);

                    continue;
                }

                // 2) Otherwise attach under ModifiedLibraries[n].ResourceTypeValueRelationships.Removed
                var libDrift = GetOrCreateLibraryDrift(drift, rel.LibraryId, logger);
                libDrift.ResourceTypeValueRelationships.Removed.Add(rel);

                logger.LogInformation(
                    "Deleted ResourceTypeValueRelationship (Src={Source}, Rel={RelGuid}, Tgt={Target}) attached to LibraryDrift.ResourceTypeValueRelationships.Removed for Library {LibraryGuid}.",
                    rel.SourceResourceTypeValue,
                    rel.RelationshipGuid,
                    rel.TargetResourceTypeValue,
                    rel.LibraryId);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // MODIFIED
        // ─────────────────────────────────────────────────────────────

        private static async Task ProcessModifiedAsync(
            TMFrameworkDriftDto drift,
            IEnumerable<ModifiedFilePathInfo> modifiedPaths,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (modifiedPaths == null)
            {
                logger.LogInformation("No modified ResourceTypeValueRelationship files detected.");
                return;
            }

            var pathList = modifiedPaths.Where(m => m != null).ToList();
            if (pathList.Count == 0)
            {
                logger.LogInformation("No modified ResourceTypeValueRelationship files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} modified ResourceTypeValueRelationship files...", pathList.Count);

            foreach (var modified in pathList)
            {
                ResourceTypeValueRelationship? baseRel;
                ResourceTypeValueRelationship? targetRel;

                try
                {
                    baseRel = await ReadSingleResourceTypeValueRelationshipAsync(
                        yamlReader,
                        modified.BaseRepositoryFilePath,
                        logger);

                    targetRel = await ReadSingleResourceTypeValueRelationshipAsync(
                        yamlReader,
                        modified.TargetRepositoryFilePath,
                        logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error reading ResourceTypeValueRelationship YAML for modified file. Base={BasePath}, Target={TargetPath}",
                        modified.BaseRepositoryFilePath,
                        modified.TargetRepositoryFilePath);
                    throw;
                }

                if (baseRel == null || targetRel == null)
                {
                    logger.LogWarning(
                        "Unable to read both base and target ResourceTypeValueRelationship for modified file. Base null={BaseNull}, Target null={TargetNull}",
                        baseRel == null,
                        targetRel == null);
                    continue;
                }

                // Compare only configured fields
                var changedFields = baseRel.CompareFields(
                    targetRel,
                    driftOptions.ResourceTypeValueRelationshipDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation(
                        "No changes detected in configured ResourceTypeValueRelationship fields for (Src={Source}, Rel={RelGuid}, Tgt={Target}). Skipping modification.",
                        targetRel.SourceResourceTypeValue,
                        targetRel.RelationshipGuid,
                        targetRel.TargetResourceTypeValue);
                    continue;
                }

                var libDrift = GetOrCreateLibraryDrift(drift, targetRel.LibraryId, logger);

                var modifiedEntity = new ModifiedEntity<ResourceTypeValueRelationship>
                {
                    Entity = baseRel,
                    ModifiedFields = changedFields
                };

                libDrift.ResourceTypeValueRelationships.Modified.Add(modifiedEntity);

                logger.LogInformation(
                    "Modified ResourceTypeValueRelationship (Src={Source}, Rel={RelGuid}, Tgt={Target}) attached to LibraryDrift.ResourceTypeValueRelationships.Modified for Library {LibraryGuid}.",
                    targetRel.SourceResourceTypeValue,
                    targetRel.RelationshipGuid,
                    targetRel.TargetResourceTypeValue,
                    targetRel.LibraryId);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        private static List<string> NormalizePathList(IEnumerable<string> paths)
        {
            return paths?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList()
                ?? new List<string>();
        }

        private static async Task<IReadOnlyCollection<ResourceTypeValueRelationship>> ReadResourceTypeValueRelationshipsAsync(
            IYamlReaderRouter yamlReader,
            IEnumerable<string> paths,
            ILogger logger,
            string operationName)
        {
            var pathList = NormalizePathList(paths);
            if (pathList.Count == 0)
                return Array.Empty<ResourceTypeValueRelationship>();

            try
            {
                var result = await yamlReader.ReadResourceTypeValueRelationsAsync(pathList);
                return result?.Where(x => x != null).ToList() ?? new List<ResourceTypeValueRelationship>();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to read ResourceTypeValueRelationship YAML files for operation '{Operation}'.",
                    operationName);
                throw;
            }
        }

        private static async Task<ResourceTypeValueRelationship?> ReadSingleResourceTypeValueRelationshipAsync(
            IYamlReaderRouter yamlReader,
            string path,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogWarning("ReadSingleResourceTypeValueRelationshipAsync called with an empty path.");
                return null;
            }

            try
            {
                var list = await yamlReader.ReadResourceTypeValueRelationsAsync(new[] { path });
                return list?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read ResourceTypeValueRelationship YAML at path: {Path}", path);
                throw;
            }
        }

        private static LibraryDriftDto GetOrCreateLibraryDrift(
            TMFrameworkDriftDto drift,
            Guid libraryGuid,
            ILogger logger)
        {
            var existing = drift.ModifiedLibraries
                .FirstOrDefault(ld => ld.LibraryGuid == libraryGuid);

            if (existing != null)
                return existing;

            var created = new LibraryDriftDto
            {
                LibraryGuid = libraryGuid
            };

            drift.ModifiedLibraries.Add(created);

            logger.LogInformation(
                "Created new LibraryDrift for LibraryGuid={LibraryGuid} to attach ResourceTypeValueRelationship drift.",
                libraryGuid);

            return created;
        }
    }
}

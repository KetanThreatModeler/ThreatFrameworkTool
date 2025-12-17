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
    public static class ResourceValuesTypeDriftProcessor
    {
        public static async Task ProcessAsync(
            TMFrameworkDriftDto drift,
            EntityFileChangeSet resourceTypeValueChanges,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (resourceTypeValueChanges == null) throw new ArgumentNullException(nameof(resourceTypeValueChanges));
            if (yamlReader == null) throw new ArgumentNullException(nameof(yamlReader));
            if (driftOptions == null) throw new ArgumentNullException(nameof(driftOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            await ProcessAddedAsync(drift, resourceTypeValueChanges.AddedFilePaths, yamlReader, logger);
            await ProcessDeletedAsync(drift, resourceTypeValueChanges.DeletedFilePaths, yamlReader, logger);
            await ProcessModifiedAsync(drift, resourceTypeValueChanges.ModifiedFiles, yamlReader, driftOptions, logger);
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
                logger.LogInformation("No added ResourceTypeValues files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} added ResourceTypeValues files...", pathList.Count);

            var values = await ReadResourceTypeValuesAsync(yamlReader, pathList, logger, "added");

            foreach (var v in values)
            {
                if (v == null)
                {
                    logger.LogWarning("Encountered null ResourceTypeValues while processing added ResourceTypeValues.");
                    continue;
                }

                // 1) Prefer AddedLibrary.ResourceTypeValues
                var addedLib = drift.AddedLibraries
                    .FirstOrDefault(l => l.Library != null && l.Library.Guid == v.LibraryId);

                if (addedLib != null)
                {
                    addedLib.ResourceTypeValues.Add(v);

                    logger.LogInformation(
                        "Added ResourceTypeValues ({ResourceTypeValue}) attached to AddedLibrary {LibraryGuid}.",
                        v.ResourceTypeValue,
                        v.LibraryId);

                    continue;
                }

                // 2) Otherwise attach under ModifiedLibraries[n].ResourceTypeValues.Added
                var libDrift = GetOrCreateLibraryDrift(drift, v.LibraryId, logger);
                libDrift.ResourceTypeValues.Added.Add(v);

                logger.LogInformation(
                    "Added ResourceTypeValues ({ResourceTypeValue}) attached to LibraryDrift.ResourceTypeValues.Added for Library {LibraryGuid}.",
                    v.ResourceTypeValue,
                    v.LibraryId);
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
                logger.LogInformation("No deleted ResourceTypeValues files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} deleted ResourceTypeValues files...", pathList.Count);

            var values = await ReadResourceTypeValuesAsync(yamlReader, pathList, logger, "deleted");

            foreach (var v in values)
            {
                if (v == null)
                {
                    logger.LogWarning("Encountered null ResourceTypeValues while processing deleted ResourceTypeValues.");
                    continue;
                }

                // 1) Prefer DeletedLibrary.ResourceTypeValues
                var deletedLib = drift.DeletedLibraries
                    .FirstOrDefault(l => l.LibraryGuid == v.LibraryId);

                if (deletedLib != null)
                {
                    deletedLib.ResourceTypeValues.Add(v);

                    logger.LogInformation(
                        "Deleted ResourceTypeValues ({ResourceTypeValue}) attached to DeletedLibrary {LibraryGuid}.",
                        v.ResourceTypeValue,
                        v.LibraryId);

                    continue;
                }

                // 2) Otherwise attach under ModifiedLibraries[n].ResourceTypeValues.Removed
                var libDrift = GetOrCreateLibraryDrift(drift, v.LibraryId, logger);
                libDrift.ResourceTypeValues.Removed.Add(v);

                logger.LogInformation(
                    "Deleted ResourceTypeValues ({ResourceTypeValue}) attached to LibraryDrift.ResourceTypeValues.Removed for Library {LibraryGuid}.",
                    v.ResourceTypeValue,
                    v.LibraryId);
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
                logger.LogInformation("No modified ResourceTypeValues files detected.");
                return;
            }

            var pathList = modifiedPaths.Where(m => m != null).ToList();
            if (pathList.Count == 0)
            {
                logger.LogInformation("No modified ResourceTypeValues files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} modified ResourceTypeValues files...", pathList.Count);

            foreach (var modified in pathList)
            {
                ResourceTypeValues? baseValue;
                ResourceTypeValues? targetValue;

                try
                {
                    baseValue = await ReadSingleResourceTypeValuesAsync(
                        yamlReader,
                        modified.BaseRepositoryFilePath,
                        logger);

                    targetValue = await ReadSingleResourceTypeValuesAsync(
                        yamlReader,
                        modified.TargetRepositoryFilePath,
                        logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error reading ResourceTypeValues YAML for modified file. Base={BasePath}, Target={TargetPath}",
                        modified.BaseRepositoryFilePath,
                        modified.TargetRepositoryFilePath);
                    throw;
                }

                if (baseValue == null || targetValue == null)
                {
                    logger.LogWarning(
                        "Unable to read both base and target ResourceTypeValues for modified file. Base null={BaseNull}, Target null={TargetNull}",
                        baseValue == null,
                        targetValue == null);
                    continue;
                }

                // Compare only configured fields
                var changedFields = baseValue.CompareFields(
                    targetValue,
                    driftOptions.ResourceTypeValuesDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation(
                        "No changes detected in configured ResourceTypeValues fields for ResourceTypeValue={ResourceTypeValue}. Skipping modification.",
                        targetValue.ResourceTypeValue);
                    continue;
                }

                var libDrift = GetOrCreateLibraryDrift(drift, targetValue.LibraryId, logger);

                var modifiedEntity = new ModifiedEntity<ResourceTypeValues>
                {
                    Entity = baseValue,
                    ModifiedFields = changedFields
                };

                libDrift.ResourceTypeValues.Modified.Add(modifiedEntity);

                logger.LogInformation(
                    "Modified ResourceTypeValues ({ResourceTypeValue}) attached to LibraryDrift.ResourceTypeValues.Modified for Library {LibraryGuid}.",
                    targetValue.ResourceTypeValue,
                    targetValue.LibraryId);
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

        private static async Task<IReadOnlyCollection<ResourceTypeValues>> ReadResourceTypeValuesAsync(
            IYamlReaderRouter yamlReader,
            IEnumerable<string> paths,
            ILogger logger,
            string operationName)
        {
            var pathList = NormalizePathList(paths);
            if (pathList.Count == 0)
                return Array.Empty<ResourceTypeValues>();

            try
            {
                var result = await yamlReader.ReadResourceTypeValuesAsync(pathList);
                return result?.Where(x => x != null).ToList() ?? new List<ResourceTypeValues>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to read ResourceTypeValues YAML files for operation '{Operation}'.",
                    operationName);
                throw;
            }
        }

        private static async Task<ResourceTypeValues?> ReadSingleResourceTypeValuesAsync(
            IYamlReaderRouter yamlReader,
            string path,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogWarning("ReadSingleResourceTypeValuesAsync called with an empty path.");
                return null;
            }

            try
            {
                var list = await yamlReader.ReadResourceTypeValuesAsync(new[] { path });
                return list?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read ResourceTypeValues YAML at path: {Path}", path);
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
                "Created new LibraryDrift for LibraryGuid={LibraryGuid} to attach ResourceTypeValues drift.",
                libraryGuid);

            return created;
        }
    }
}

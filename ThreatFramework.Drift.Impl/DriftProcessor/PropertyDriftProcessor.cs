using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor
{
    public static class PropertyDriftProcessor
    {
        public static async Task ProcessAsync(
            TMFrameworkDriftDto drift,
            EntityFileChangeSet propertyChanges,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (propertyChanges == null) throw new ArgumentNullException(nameof(propertyChanges));
            if (yamlReader == null) throw new ArgumentNullException(nameof(yamlReader));
            if (driftOptions == null) throw new ArgumentNullException(nameof(driftOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            await ProcessAddedAsync(drift, propertyChanges.AddedFilePaths, yamlReader, logger);
            await ProcessDeletedAsync(drift, propertyChanges.DeletedFilePaths, yamlReader, logger);
            await ProcessModifiedAsync(drift, propertyChanges.ModifiedFiles, yamlReader, driftOptions, logger);
        }

        // ─────────────────────────────────────────────────────────────
        // ADDED PROPERTIES
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
                logger.LogInformation("No added property files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} added property files...", pathList.Count);

            var properties = await ReadPropertiesAsync(yamlReader, pathList, logger, "added");

            foreach (var property in properties)
            {
                if (property == null)
                {
                    logger.LogWarning("Encountered null Property while processing added properties.");
                    continue;
                }

                // 1) Try to hang it under AddedLibrary.Properties
                var addedLib = drift.AddedLibraries
                    .FirstOrDefault(l => l.Library != null && l.Library.Guid == property.LibraryGuid);

                if (addedLib != null)
                {
                    addedLib.Properties.Add(property);

                    logger.LogInformation(
                        "Added Property {PropertyGuid} attached to AddedLibrary {LibraryGuid}.",
                        property.Guid,
                        property.LibraryGuid);

                    continue;
                }

                // 2) Otherwise under LibraryDrift.Properties.Added
                var libDrift = GetOrCreateLibraryDrift(drift, property.LibraryGuid, logger);

                libDrift.Properties.Added.Add(property);

                logger.LogInformation(
                    "Added Property {PropertyGuid} attached to LibraryDrift.Properties.Added for Library {LibraryGuid}.",
                    property.Guid,
                    property.LibraryGuid);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // DELETED PROPERTIES
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
                logger.LogInformation("No deleted property files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} deleted property files...", pathList.Count);

            var properties = await ReadPropertiesAsync(yamlReader, pathList, logger, "deleted");

            foreach (var property in properties)
            {
                if (property == null)
                {
                    logger.LogWarning("Encountered null Property while processing deleted properties.");
                    continue;
                }

                // 1) Prefer DeletedLibrary.Properties
                var deletedLib = drift.DeletedLibraries
                    .FirstOrDefault(l => l.LibraryGuid == property.LibraryGuid);

                if (deletedLib != null)
                {
                    deletedLib.Properties.Add(property);

                    logger.LogInformation(
                        "Deleted Property {PropertyGuid} attached to DeletedLibrary {LibraryGuid}.",
                        property.Guid,
                        property.LibraryGuid);

                    continue;
                }

                // 2) Otherwise under LibraryDrift.Properties.Removed
                var libDrift = GetOrCreateLibraryDrift(drift, property.LibraryGuid, logger);

                libDrift.Properties.Removed.Add(property);

                logger.LogInformation(
                    "Deleted Property {PropertyGuid} attached to LibraryDrift.Properties.Removed for Library {LibraryGuid}.",
                    property.Guid,
                    property.LibraryGuid);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // MODIFIED PROPERTIES
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
                logger.LogInformation("No modified property files detected.");
                return;
            }

            var pathList = modifiedPaths.Where(m => m != null).ToList();
            if (pathList.Count == 0)
            {
                logger.LogInformation("No modified property files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} modified property files...", pathList.Count);

            foreach (var modified in pathList)
            {
                Property? baseProperty;
                Property? targetProperty;

                try
                {
                    baseProperty = await ReadSinglePropertyAsync(
                        yamlReader,
                        modified.BaseRepositoryFilePath,
                        logger);

                    targetProperty = await ReadSinglePropertyAsync(
                        yamlReader,
                        modified.TargetRepositoryFilePath,
                        logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error reading Property YAML for modified file. Base={BasePath}, Target={TargetPath}",
                        modified.BaseRepositoryFilePath,
                        modified.TargetRepositoryFilePath);
                    throw;
                }

                if (baseProperty == null || targetProperty == null)
                {
                    logger.LogWarning(
                        "Unable to read both base and target Property for modified file. Base null={BaseNull}, Target null={TargetNull}",
                        baseProperty == null,
                        targetProperty == null);
                    continue;
                }

                // Compare only configured fields
                var changedFields = baseProperty.CompareFields(
                    targetProperty,
                    driftOptions.PropertyDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation(
                        "No changes detected in configured Property fields for {PropertyGuid}. Skipping modification.",
                        baseProperty.Guid);
                    continue;
                }

                // Always belong to a modified library (create if needed)
                var libDrift = GetOrCreateLibraryDrift(drift, targetProperty.LibraryGuid, logger);

                var modifiedEntity = new ModifiedEntity<Property>
                {
                    Entity = baseProperty,
                    ModifiedFields = changedFields,
                };

                libDrift.Properties.Modified.Add(modifiedEntity);

                logger.LogInformation(
                    "Modified Property {PropertyGuid} attached to LibraryDrift.Properties.Modified for Library {LibraryGuid}.",
                    targetProperty.Guid,
                    targetProperty.LibraryGuid);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Shared helpers (DRY, reusable across other entity processors)
        // ─────────────────────────────────────────────────────────────

        private static List<string> NormalizePathList(IEnumerable<string> paths)
        {
            return paths?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList()
                ?? new List<string>();
        }

        private static async Task<IReadOnlyCollection<Property>> ReadPropertiesAsync(
            IYamlReaderRouter yamlReader,
            IEnumerable<string> paths,
            ILogger logger,
            string operationName)
        {
            var pathList = NormalizePathList(paths);
            if (pathList.Count == 0)
            {
                return Array.Empty<Property>();
            }

            try
            {
                var result = await yamlReader.ReadPropertiesAsync(pathList);
                return result?.Where(p => p != null).ToList() ?? new List<Property>();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to read Property YAML files for operation '{Operation}'.",
                    operationName);
                throw;
            }
        }

        private static async Task<Property?> ReadSinglePropertyAsync(
            IYamlReaderRouter yamlReader,
            string path,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogWarning("ReadSinglePropertyAsync called with an empty path.");
                return null;
            }

            try
            {
                var list = await yamlReader.ReadPropertiesAsync(new[] { path });
                return list?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read Property YAML at path: {Path}", path);
                throw;
            }
        }

        /// <summary>
        /// Finds an existing LibraryDrift for a given libraryGuid, or creates one and
        /// adds it to TMFrameworkDrift.ModifiedLibraries.
        /// </summary>
        private static LibraryDriftDto GetOrCreateLibraryDrift(
            TMFrameworkDriftDto drift,
            Guid libraryGuid,
            ILogger logger)
        {
            var existing = drift.ModifiedLibraries
                .FirstOrDefault(ld => ld.LibraryGuid == libraryGuid);

            if (existing != null)
            {
                return existing;
            }

            var newDrift = new LibraryDriftDto
            {
                LibraryGuid = libraryGuid
            };

            drift.ModifiedLibraries.Add(newDrift);

            logger.LogInformation(
                "Created new LibraryDrift for LibraryGuid={LibraryGuid} to attach Property drift.",
                libraryGuid);

            return newDrift;
        }
    }
}
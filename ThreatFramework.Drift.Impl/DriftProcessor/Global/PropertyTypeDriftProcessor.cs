using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core;
using ThreatFramework.Drift.Contract.CoreEntityDriftService;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor.Global
{
    public static class PropertyTypeDriftProcessor
    {
        public static async Task ProcessAsync(
            TMFrameworkDrift drift,
            EntityFileChangeSet propertyTypeChanges,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (propertyTypeChanges == null) throw new ArgumentNullException(nameof(propertyTypeChanges));
            if (yamlReader == null) throw new ArgumentNullException(nameof(yamlReader));
            if (driftOptions == null) throw new ArgumentNullException(nameof(driftOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            // Added
            await ProcessAddedAsync(
                drift,
                propertyTypeChanges.AddedFilePaths,
                yamlReader,
                logger);

            // Deleted
            await ProcessDeletedAsync(
                drift,
                propertyTypeChanges.DeletedFilePaths,
                yamlReader,
                logger);

            // Modified
            await ProcessModifiedAsync(
                drift,
                propertyTypeChanges.ModifiedFiles,
                yamlReader,
                driftOptions,
                logger);
        }

        // ─────────────────────────────────────────────────────────────
        // ADDED PROPERTY TYPES
        // ─────────────────────────────────────────────────────────────

        private static async Task ProcessAddedAsync(
            TMFrameworkDrift drift,
            IEnumerable<string> addedPaths,
            IYamlReaderRouter yamlReader,
            ILogger logger)
        {
            var pathList = NormalizePathList(addedPaths);
            if (pathList.Count == 0)
            {
                logger.LogInformation("No added PropertyType files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} added PropertyType files...", pathList.Count);

            var propertyTypes = await ReadPropertyTypesAsync(yamlReader, pathList, logger, "added");

            foreach (var propertyType in propertyTypes)
            {
                if (propertyType == null)
                {
                    logger.LogWarning("Encountered null PropertyType while processing added PropertyTypes.");
                    continue;
                }

                // Global entity: attach directly under TMFrameworkDrift.PropertyTypes.Added
                // Assumes TMFrameworkDrift.PropertyTypes is initialized.
                drift.Global.PropertyTypes.Added.Add(propertyType);

                logger.LogInformation(
                    "Added PropertyType {PropertyTypeGuid} ({Name}) attached to TMFrameworkDrift.PropertyTypes.Added.",
                    propertyType.Guid,
                    propertyType.Name);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // DELETED PROPERTY TYPES
        // ─────────────────────────────────────────────────────────────

        private static async Task ProcessDeletedAsync(
            TMFrameworkDrift drift,
            IEnumerable<string> deletedPaths,
            IYamlReaderRouter yamlReader,
            ILogger logger)
        {
            var pathList = NormalizePathList(deletedPaths);
            if (pathList.Count == 0)
            {
                logger.LogInformation("No deleted PropertyType files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} deleted PropertyType files...", pathList.Count);

            var propertyTypes = await ReadPropertyTypesAsync(yamlReader, pathList, logger, "deleted");

            foreach (var propertyType in propertyTypes)
            {
                if (propertyType == null)
                {
                    logger.LogWarning("Encountered null PropertyType while processing deleted PropertyTypes.");
                    continue;
                }

                // Global entity: attach directly under TMFrameworkDrift.PropertyTypes.Removed
                drift.Global.PropertyTypes.Removed.Add(propertyType);

                logger.LogInformation(
                    "Deleted PropertyType {PropertyTypeGuid} ({Name}) attached to TMFrameworkDrift.PropertyTypes.Removed.",
                    propertyType.Guid,
                    propertyType.Name);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // MODIFIED PROPERTY TYPES
        // ─────────────────────────────────────────────────────────────

        private static async Task ProcessModifiedAsync(
            TMFrameworkDrift drift,
            IEnumerable<ModifiedFilePathInfo> modifiedPaths,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (modifiedPaths == null)
            {
                logger.LogInformation("No modified PropertyType files detected.");
                return;
            }

            var pathList = modifiedPaths.Where(m => m != null).ToList();
            if (pathList.Count == 0)
            {
                logger.LogInformation("No modified PropertyType files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} modified PropertyType files...", pathList.Count);

            foreach (var modified in pathList)
            {
                PropertyType? basePropertyType;
                PropertyType? targetPropertyType;

                try
                {
                    basePropertyType = await ReadSinglePropertyTypeAsync(
                        yamlReader,
                        modified.BaseRepositoryFilePath,
                        logger);

                    targetPropertyType = await ReadSinglePropertyTypeAsync(
                        yamlReader,
                        modified.TargetRepositoryFilePath,
                        logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error reading PropertyType YAML for modified file. Base={BasePath}, Target={TargetPath}",
                        modified.BaseRepositoryFilePath,
                        modified.TargetRepositoryFilePath);
                    throw;
                }

                if (basePropertyType == null || targetPropertyType == null)
                {
                    logger.LogWarning(
                        "Unable to read both base and target PropertyType for modified file. Base null={BaseNull}, Target null={TargetNull}",
                        basePropertyType == null,
                        targetPropertyType == null);
                    continue;
                }

                // Compare only configured fields (e.g. driftOptions.PropertyTypeDefaultFields)
                var changedFields = basePropertyType.CompareFields(
                    targetPropertyType,
                    driftOptions.PropertyTypeDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation(
                        "No changes detected in configured PropertyType fields for {PropertyTypeGuid}. Skipping modification.",
                        basePropertyType.Guid);
                    continue;
                }

                var modifiedEntity = new ModifiedEntity<PropertyType>
                {
                    Entity = basePropertyType,
                    ModifiedFields = changedFields,
                };

                drift.Global.PropertyTypes.Modified.Add(modifiedEntity);

                logger.LogInformation(
                    "Modified PropertyType {PropertyTypeGuid} ({Name}) attached to TMFrameworkDrift.PropertyTypes.Modified.",
                    targetPropertyType.Guid,
                    targetPropertyType.Name);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Shared helpers – local to this processor
        // ─────────────────────────────────────────────────────────────

        private static List<string> NormalizePathList(IEnumerable<string> paths)
        {
            return paths?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList()
                ?? new List<string>();
        }

        private static async Task<IReadOnlyCollection<PropertyType>> ReadPropertyTypesAsync(
            IYamlReaderRouter yamlReader,
            IEnumerable<string> paths,
            ILogger logger,
            string operationName)
        {
            var pathList = NormalizePathList(paths);
            if (pathList.Count == 0)
            {
                return Array.Empty<PropertyType>();
            }

            try
            {
                // Assumes IYamlReaderRouter exposes ReadPropertyTypesAsync
                var result = await yamlReader.ReadPropertyTypesAsync(pathList);
                return result?.Where(pt => pt != null).ToList() ?? new List<PropertyType>();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to read PropertyType YAML files for operation '{Operation}'.",
                    operationName);
                throw;
            }
        }

        private static async Task<PropertyType?> ReadSinglePropertyTypeAsync(
            IYamlReaderRouter yamlReader,
            string path,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogWarning("ReadSinglePropertyTypeAsync called with an empty path.");
                return null;
            }

            try
            {
                var list = await yamlReader.ReadPropertyTypesAsync(new[] { path });
                return list?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read PropertyType YAML at path: {Path}", path);
                throw;
            }
        }
    }
}

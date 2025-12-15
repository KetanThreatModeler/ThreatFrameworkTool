using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Model.Global;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor.Global
{
    public static class ComponentTypeDriftProcessor
    {
        public static async Task ProcessAsync(
            TMFrameworkDriftDto drift,
            EntityFileChangeSet componentTypeChanges,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (componentTypeChanges == null) throw new ArgumentNullException(nameof(componentTypeChanges));
            if (yamlReader == null) throw new ArgumentNullException(nameof(yamlReader));
            if (driftOptions == null) throw new ArgumentNullException(nameof(driftOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            // Added
            await ProcessAddedAsync(
                drift,
                componentTypeChanges.AddedFilePaths,
                yamlReader,
                logger);

            // Deleted
            await ProcessDeletedAsync(
                drift,
                componentTypeChanges.DeletedFilePaths,
                yamlReader,
                logger);

            // Modified
            await ProcessModifiedAsync(
                drift,
                componentTypeChanges.ModifiedFiles,
                yamlReader,
                driftOptions,
                logger);
        }

        // ─────────────────────────────────────────────────────────────
        // ADDED COMPONENT TYPES
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
                logger.LogInformation("No added ComponentType files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} added ComponentType files...", pathList.Count);

            var componentTypes = await ReadComponentTypesAsync(yamlReader, pathList, logger, "added");

            foreach (var componentType in componentTypes)
            {
                if (componentType == null)
                {
                    logger.LogWarning("Encountered null ComponentType while processing added ComponentTypes.");
                    continue;
                }

                // Global entity: attach directly under TMFrameworkDrift.ComponentTypes.Added
                drift.Global.ComponentTypes.Added.Add(componentType);

                logger.LogInformation(
                    "Added ComponentType {ComponentTypeGuid} ({Name}) attached to TMFrameworkDrift.ComponentTypes.Added.",
                    componentType.Guid,
                    componentType.Name);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // DELETED COMPONENT TYPES
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
                logger.LogInformation("No deleted ComponentType files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} deleted ComponentType files...", pathList.Count);

            var componentTypes = await ReadComponentTypesAsync(yamlReader, pathList, logger, "deleted");

            foreach (var componentType in componentTypes)
            {
                if (componentType == null)
                {
                    logger.LogWarning("Encountered null ComponentType while processing deleted ComponentTypes.");
                    continue;
                }

                // Global entity: attach directly under TMFrameworkDrift.ComponentTypes.Removed
                drift.Global.ComponentTypes.Removed.Add(componentType);

                logger.LogInformation(
                    "Deleted ComponentType {ComponentTypeGuid} ({Name}) attached to TMFrameworkDrift.ComponentTypes.Removed.",
                    componentType.Guid,
                    componentType.Name);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // MODIFIED COMPONENT TYPES
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
                logger.LogInformation("No modified ComponentType files detected.");
                return;
            }

            var pathList = modifiedPaths.Where(m => m != null).ToList();
            if (pathList.Count == 0)
            {
                logger.LogInformation("No modified ComponentType files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} modified ComponentType files...", pathList.Count);

            foreach (var modified in pathList)
            {
                ComponentType? baseComponentType;
                ComponentType? targetComponentType;

                try
                {
                    baseComponentType = await ReadSingleComponentTypeAsync(
                        yamlReader,
                        modified.BaseRepositoryFilePath,
                        logger);

                    targetComponentType = await ReadSingleComponentTypeAsync(
                        yamlReader,
                        modified.TargetRepositoryFilePath,
                        logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error reading ComponentType YAML for modified file. Base={BasePath}, Target={TargetPath}",
                        modified.BaseRepositoryFilePath,
                        modified.TargetRepositoryFilePath);
                    throw;
                }

                if (baseComponentType == null || targetComponentType == null)
                {
                    logger.LogWarning(
                        "Unable to read both base and target ComponentType for modified file. Base null={BaseNull}, Target null={TargetNull}",
                        baseComponentType == null,
                        targetComponentType == null);
                    continue;
                }

                // Compare only configured fields (e.g. driftOptions.ComponentTypeDefaultFields)
                var changedFields = targetComponentType.CompareFields(
                    baseComponentType,
                    driftOptions.ComponentTypeDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation(
                        "No changes detected in configured ComponentType fields for {ComponentTypeGuid}. Skipping modification.",
                        baseComponentType.Guid);
                    continue;
                }

                var modifiedEntity = new ModifiedEntity<ComponentType>
                {
                    Entity = targetComponentType,
                    ModifiedFields = changedFields,
                };

                drift.Global.ComponentTypes.Modified.Add(modifiedEntity);

                logger.LogInformation(
                    "Modified ComponentType {ComponentTypeGuid} ({Name}) attached to TMFrameworkDrift.ComponentTypes.Modified.",
                    targetComponentType.Guid,
                    targetComponentType.Name);
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

        private static async Task<IReadOnlyCollection<ComponentType>> ReadComponentTypesAsync(
            IYamlReaderRouter yamlReader,
            IEnumerable<string> paths,
            ILogger logger,
            string operationName)
        {
            var pathList = NormalizePathList(paths);
            if (pathList.Count == 0)
            {
                return Array.Empty<ComponentType>();
            }

            try
            {
                // Assumes IYamlReaderRouter exposes ReadComponentTypesAsync
                var result = await yamlReader.ReadComponentTypeAsync(pathList);
                return result?.Where(ct => ct != null).ToList() ?? new List<ComponentType>();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to read ComponentType YAML files for operation '{Operation}'.",
                    operationName);
                throw;
            }
        }

        private static async Task<ComponentType?> ReadSingleComponentTypeAsync(
            IYamlReaderRouter yamlReader,
            string path,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogWarning("ReadSingleComponentTypeAsync called with an empty path.");
                return null;
            }

            try
            {
                var list = await yamlReader.ReadComponentTypeAsync(new[] { path });
                return list?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read ComponentType YAML at path: {Path}", path);
                throw;
            }
        }
    }
}

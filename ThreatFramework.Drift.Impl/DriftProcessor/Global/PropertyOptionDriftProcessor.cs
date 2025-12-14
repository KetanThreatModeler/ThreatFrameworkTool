using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor.Global
{
    public static class PropertyOptionDriftProcessor
    {
        public static async Task ProcessAsync(
            TMFrameworkDriftDto drift,
            EntityFileChangeSet propertyOptionChanges,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (propertyOptionChanges == null) throw new ArgumentNullException(nameof(propertyOptionChanges));
            if (yamlReader == null) throw new ArgumentNullException(nameof(yamlReader));
            if (driftOptions == null) throw new ArgumentNullException(nameof(driftOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            // Added
            await ProcessAddedAsync(
                drift,
                propertyOptionChanges.AddedFilePaths,
                yamlReader,
                logger);

            // Deleted
            await ProcessDeletedAsync(
                drift,
                propertyOptionChanges.DeletedFilePaths,
                yamlReader,
                logger);

            // Modified
            await ProcessModifiedAsync(
                drift,
                propertyOptionChanges.ModifiedFiles,
                yamlReader,
                driftOptions,
                logger);
        }

        // ─────────────────────────────────────────────────────────────
        // ADDED PROPERTY OPTIONS
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
                logger.LogInformation("No added PropertyOption files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} added PropertyOption files...", pathList.Count);

            var propertyOptions = await ReadPropertyOptionsAsync(yamlReader, pathList, logger, "added");

            foreach (var option in propertyOptions)
            {
                if (option == null)
                {
                    logger.LogWarning("Encountered null PropertyOption while processing added PropertyOptions.");
                    continue;
                }

                // Global entity: attach directly under TMFrameworkDrift.PropertyOptions.Added
                drift.Global.PropertyOptions.Added.Add(option);

                logger.LogInformation(
                    "Added PropertyOption {PropertyOptionGuid} (PropertyGuid={PropertyGuid}) attached to TMFrameworkDrift.PropertyOptions.Added.",
                    option.Guid,
                    option.PropertyGuid);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // DELETED PROPERTY OPTIONS
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
                logger.LogInformation("No deleted PropertyOption files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} deleted PropertyOption files...", pathList.Count);

            var propertyOptions = await ReadPropertyOptionsAsync(yamlReader, pathList, logger, "deleted");

            foreach (var option in propertyOptions)
            {
                if (option == null)
                {
                    logger.LogWarning("Encountered null PropertyOption while processing deleted PropertyOptions.");
                    continue;
                }

                // Global entity: attach directly under TMFrameworkDrift.PropertyOptions.Removed
                drift.Global.PropertyOptions.Removed.Add(option);

                logger.LogInformation(
                    "Deleted PropertyOption {PropertyOptionGuid} (PropertyGuid={PropertyGuid}) attached to TMFrameworkDrift.PropertyOptions.Removed.",
                    option.Guid,
                    option.PropertyGuid);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // MODIFIED PROPERTY OPTIONS
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
                logger.LogInformation("No modified PropertyOption files detected.");
                return;
            }

            var pathList = modifiedPaths.Where(m => m != null).ToList();
            if (pathList.Count == 0)
            {
                logger.LogInformation("No modified PropertyOption files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} modified PropertyOption files...", pathList.Count);

            foreach (var modified in pathList)
            {
                PropertyOption? baseOption;
                PropertyOption? targetOption;

                try
                {
                    baseOption = await ReadSinglePropertyOptionAsync(
                        yamlReader,
                        modified.BaseRepositoryFilePath,
                        logger);

                    targetOption = await ReadSinglePropertyOptionAsync(
                        yamlReader,
                        modified.TargetRepositoryFilePath,
                        logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error reading PropertyOption YAML for modified file. Base={BasePath}, Target={TargetPath}",
                        modified.BaseRepositoryFilePath,
                        modified.TargetRepositoryFilePath);
                    throw;
                }

                if (baseOption == null || targetOption == null)
                {
                    logger.LogWarning(
                        "Unable to read both base and target PropertyOption for modified file. Base null={BaseNull}, Target null={TargetNull}",
                        baseOption == null,
                        targetOption == null);
                    continue;
                }

                // Compare only configured fields (e.g. driftOptions.PropertyOptionDefaultFields)
                var changedFields = baseOption.CompareFields(
                    targetOption,
                    driftOptions.PropertyOptionDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation(
                        "No changes detected in configured PropertyOption fields for {PropertyOptionGuid}. Skipping modification.",
                        baseOption.Guid);
                    continue;
                }

                var modifiedEntity = new ModifiedEntity<PropertyOption>
                {
                    Entity = baseOption,
                    ModifiedFields = changedFields,
                };

                drift.Global.PropertyOptions.Modified.Add(modifiedEntity);

                logger.LogInformation(
                    "Modified PropertyOption {PropertyOptionGuid} (PropertyGuid={PropertyGuid}) attached to TMFrameworkDrift.PropertyOptions.Modified.",
                    targetOption.Guid,
                    targetOption.PropertyGuid);
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

        private static async Task<IReadOnlyCollection<PropertyOption>> ReadPropertyOptionsAsync(
            IYamlReaderRouter yamlReader,
            IEnumerable<string> paths,
            ILogger logger,
            string operationName)
        {
            var pathList = NormalizePathList(paths);
            if (pathList.Count == 0)
            {
                return Array.Empty<PropertyOption>();
            }

            try
            {
                // Assumes IYamlReaderRouter exposes ReadPropertyOptionsAsync
                var result = await yamlReader.ReadPropertyOptionsAsync(pathList);
                return result?.Where(po => po != null).ToList() ?? new List<PropertyOption>();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to read PropertyOption YAML files for operation '{Operation}'.",
                    operationName);
                throw;
            }
        }

        private static async Task<PropertyOption?> ReadSinglePropertyOptionAsync(
            IYamlReaderRouter yamlReader,
            string path,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogWarning("ReadSinglePropertyOptionAsync called with an empty path.");
                return null;
            }

            try
            {
                var list = await yamlReader.ReadPropertyOptionsAsync(new[] { path });
                return list?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read PropertyOption YAML at path: {Path}", path);
                throw;
            }
        }
    }
}
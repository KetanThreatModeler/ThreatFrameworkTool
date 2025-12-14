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
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor
{
    public static class LibraryDriftProcessor
    {
        public static async Task ProcessAsync(
            TMFrameworkDriftDto drift,
            EntityFileChangeSet libraryChanges,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (libraryChanges == null) throw new ArgumentNullException(nameof(libraryChanges));
            if (yamlReader == null) throw new ArgumentNullException(nameof(yamlReader));
            if (driftOptions == null) throw new ArgumentNullException(nameof(driftOptions));

            await ProcessAddedAsync(drift, libraryChanges.AddedFilePaths, yamlReader, logger);
            await ProcessDeletedAsync(drift, libraryChanges.DeletedFilePaths, yamlReader, logger);
            await ProcessModifiedAsync(drift, libraryChanges.ModifiedFiles, yamlReader, driftOptions, logger);
        }

        // ------------------------------------------------------------
        // ADDED
        // ------------------------------------------------------------
        private static async Task ProcessAddedAsync(
            TMFrameworkDriftDto drift,
            IReadOnlyCollection<string> addedPaths,
            IYamlReaderRouter yamlReader,
            ILogger logger)
        {
            if (addedPaths == null || addedPaths.Count == 0)
                return;

            logger.LogInformation("Processing {Count} added library files...", addedPaths.Count);

            var libs = await yamlReader.ReadLibrariesAsync(addedPaths);

            foreach (var lib in libs)
            {
                if (lib == null)
                {
                    logger.LogWarning("Encountered null Library while processing added libraries.");
                    continue;
                }

                drift.AddedLibraries.Add(new AddedLibraryDto
                {
                    Library = lib
                });

                logger.LogInformation("Added Library: {Name} ({Guid})", lib.Name, lib.Guid);
            }
        }

        // ------------------------------------------------------------
        // DELETED
        // ------------------------------------------------------------
        private static async Task ProcessDeletedAsync(
            TMFrameworkDriftDto drift,
            IReadOnlyCollection<string> deletedPaths,
            IYamlReaderRouter yamlReader,
            ILogger logger)
        {
            if (deletedPaths == null || deletedPaths.Count == 0)
                return;

            logger.LogInformation("Processing {Count} deleted library files...", deletedPaths.Count);

            var libs = await yamlReader.ReadLibrariesAsync(deletedPaths);

            foreach (var lib in libs)
            {
                if (lib == null)
                {
                    logger.LogWarning("Encountered null Library while processing deleted libraries.");
                    continue;
                }

                drift.DeletedLibraries.Add(new DeletedLibraryDto
                {
                    LibraryGuid = lib.Guid,
                    LibraryName = lib.Name
                });

                logger.LogInformation("Deleted Library: {Name} ({Guid})", lib.Name, lib.Guid);
            }
        }

        // ------------------------------------------------------------
        // MODIFIED
        // ------------------------------------------------------------
        private static async Task ProcessModifiedAsync(
            TMFrameworkDriftDto drift,
            IReadOnlyCollection<ModifiedFilePathInfo> modifiedPaths,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (modifiedPaths == null || modifiedPaths.Count == 0)
                return;

            logger.LogInformation("Processing {Count} modified library files...", modifiedPaths.Count);

            foreach (var modified in modifiedPaths)
            {
                if (modified == null)
                {
                    logger.LogWarning("Null ModifiedFilePathInfo encountered.");
                    continue;
                }

                var baseLib = await ReadSingleAsync(yamlReader, modified.BaseRepositoryFilePath, logger);
                var targetLib = await ReadSingleAsync(yamlReader, modified.TargetRepositoryFilePath, logger);

                if (baseLib == null || targetLib == null)
                {
                    logger.LogWarning("Unable to read both sides of modified library file.");
                    continue;
                }

                // Compare ONLY fields defined in config
                var changedFields = baseLib.CompareFields(targetLib, driftOptions.LibraryDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation("Modified library had no changes in configured fields -> Ignored");
                    continue;
                }

                drift.ModifiedLibraries.Add(new LibraryDriftDto
                {
                    LibraryGuid = baseLib.Guid,
                    LibraryChanges = changedFields
                });

                logger.LogInformation(
                    "Library modified: {Name} ({Guid}). {Count} fields changed.",
                    baseLib.Name,
                    baseLib.Guid,
                    changedFields.Count);
            }
        }

        private static async Task<Library?> ReadSingleAsync(
            IYamlReaderRouter yamlReader,
            string path,
            ILogger logger)
        {
            try
            {
                var result = await yamlReader.ReadLibrariesAsync(new[] { path });
                return result?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read library YAML at path: {Path}", path);
                return null;
            }
        }
    }
}

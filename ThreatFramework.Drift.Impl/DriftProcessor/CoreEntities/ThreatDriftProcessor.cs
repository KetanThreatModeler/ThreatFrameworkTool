using Microsoft.Extensions.Logging;
using ThreatFramework.Core;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor.CoreEntities
{
    public static class ThreatDriftProcessor
    {
        public static async Task ProcessAsync(
            TMFrameworkDriftDto drift,
            EntityFileChangeSet threatChanges,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (threatChanges == null) throw new ArgumentNullException(nameof(threatChanges));
            if (yamlReader == null) throw new ArgumentNullException(nameof(yamlReader));
            if (driftOptions == null) throw new ArgumentNullException(nameof(driftOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            await ProcessAddedAsync(drift, threatChanges.AddedFilePaths, yamlReader, logger);
            await ProcessDeletedAsync(drift, threatChanges.DeletedFilePaths, yamlReader, logger);
            await ProcessModifiedAsync(drift, threatChanges.ModifiedFiles, yamlReader, driftOptions, logger);
        }

        // ─────────────────────────────────────────────────────────────
        // ADDED THREATS
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
                logger.LogInformation("No added threat files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} added threat files...", pathList.Count);

            var threats = await ReadThreatsAsync(yamlReader, pathList, logger, "added");

            foreach (var threat in threats)
            {
                if (threat == null)
                {
                    logger.LogWarning("Encountered null Threat while processing added threats.");
                    continue;
                }

                // 1) Try to hang it under AddedLibrary.Threats
                var addedLib = drift.AddedLibraries
                    .FirstOrDefault(l => l.Library != null && l.Library.Guid == threat.LibraryGuid);

                if (addedLib != null)
                {
                    addedLib.Threats.Add(new AddedThreatDto
                    {
                        Threat = threat
                    });

                    logger.LogInformation(
                        "Added Threat {ThreatGuid} attached to AddedLibrary {LibraryGuid}.",
                        threat.Guid,
                        threat.LibraryGuid);

                    continue;
                }

                // 2) Otherwise under LibraryDrift.Threats.Added
                var libDrift = GetOrCreateLibraryDrift(drift, threat.LibraryGuid, logger);

                libDrift.Threats.Added.Add(new AddedThreatDto
                {
                    Threat = threat
                });

                logger.LogInformation(
                    "Added Threat {ThreatGuid} attached to LibraryDrift.Threats.Added for Library {LibraryGuid}.",
                    threat.Guid,
                    threat.LibraryGuid);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // DELETED THREATS
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
                logger.LogInformation("No deleted threat files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} deleted threat files...", pathList.Count);

            var threats = await ReadThreatsAsync(yamlReader, pathList, logger, "deleted");

            foreach (var threat in threats)
            {
                if (threat == null)
                {
                    logger.LogWarning("Encountered null Threat while processing deleted threats.");
                    continue;
                }

                // 1) Prefer DeletedLibrary.Threats
                var deletedLib = drift.DeletedLibraries
                    .FirstOrDefault(l => l.LibraryGuid == threat.LibraryGuid);

                if (deletedLib != null)
                {
                    deletedLib.Threats.Add(new RemovedThreatDto
                    {
                        Threat = threat
                    });

                    logger.LogInformation(
                        "Deleted Threat {ThreatGuid} attached to DeletedLibrary {LibraryGuid}.",
                        threat.Guid,
                        threat.LibraryGuid);

                    continue;
                }

                // 2) Otherwise under LibraryDrift.Threats.Removed
                var libDrift = GetOrCreateLibraryDrift(drift, threat.LibraryGuid, logger);

                libDrift.Threats.Removed.Add(new RemovedThreatDto
                {
                    Threat = threat
                });

                logger.LogInformation(
                    "Deleted Threat {ThreatGuid} attached to LibraryDrift.Threats.Removed for Library {LibraryGuid}.",
                    threat.Guid,
                    threat.LibraryGuid);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // MODIFIED THREATS
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
                logger.LogInformation("No modified threat files detected.");
                return;
            }

            var pathList = modifiedPaths.Where(m => m != null).ToList();
            if (pathList.Count == 0)
            {
                logger.LogInformation("No modified threat files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} modified threat files...", pathList.Count);

            foreach (var modified in pathList)
            {
                Threat? baseThreat;
                Threat? targetThreat;

                try
                {
                    baseThreat = await ReadSingleThreatAsync(
                        yamlReader,
                        modified.BaseRepositoryFilePath,
                        logger);

                    targetThreat = await ReadSingleThreatAsync(
                        yamlReader,
                        modified.TargetRepositoryFilePath,
                        logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error reading Threat YAML for modified file. Base={BasePath}, Target={TargetPath}",
                        modified.BaseRepositoryFilePath,
                        modified.TargetRepositoryFilePath);
                    throw;
                }

                if (baseThreat == null || targetThreat == null)
                {
                    logger.LogWarning(
                        "Unable to read both base and target Threat for modified file. Base null={BaseNull}, Target null={TargetNull}",
                        baseThreat == null,
                        targetThreat == null);
                    continue;
                }

                // Compare only configured fields
                var changedFields = targetThreat.CompareFields(
                    baseThreat,
                    driftOptions.ThreatDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation(
                        "No changes detected in configured Threat fields for {ThreatGuid}. Skipping modification.",
                        baseThreat.Guid);
                    continue;
                }

                // Always belong to a modified library (create if needed)
                var libDrift = GetOrCreateLibraryDrift(drift, targetThreat.LibraryGuid, logger);

                libDrift.Threats.Modified.Add(new ModifiedThreatDto
                {
                    Threat = targetThreat,
                    ChangedFields = changedFields
                });

                logger.LogInformation(
                    "Modified Threat {ThreatGuid} attached to LibraryDrift.Threats.Modified for Library {LibraryGuid}.",
                    targetThreat.Guid,
                    targetThreat.LibraryGuid);
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

        private static async Task<IReadOnlyCollection<Threat>> ReadThreatsAsync(
            IYamlReaderRouter yamlReader,
            IEnumerable<string> paths,
            ILogger logger,
            string operationName)
        {
            var pathList = NormalizePathList(paths);
            if (pathList.Count == 0)
            {
                return Array.Empty<Threat>();
            }

            try
            {
                var result = await yamlReader.ReadThreatsAsync(pathList);
                return result?.Where(t => t != null).ToList() ?? new List<Threat>();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to read Threat YAML files for operation '{Operation}'.",
                    operationName);
                throw;
            }
        }

        private static async Task<Threat?> ReadSingleThreatAsync(
            IYamlReaderRouter yamlReader,
            string path,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogWarning("ReadSingleThreatAsync called with an empty path.");
                return null;
            }

            try
            {
                var list = await yamlReader.ReadThreatsAsync(new[] { path });
                return list?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read Threat YAML at path: {Path}", path);
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
                "Created new LibraryDrift for LibraryGuid={LibraryGuid} to attach Threat drift.",
                libraryGuid);

            return newDrift;
        }
    }
}
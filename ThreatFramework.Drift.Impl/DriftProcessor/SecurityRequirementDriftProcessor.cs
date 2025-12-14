using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract.CoreEntityDriftService;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor
{
    public static class SecurityRequirementDriftProcessor
    {
        public static async Task ProcessAsync(
            TMFrameworkDrift drift,
            EntityFileChangeSet securityRequirementChanges,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (securityRequirementChanges == null) throw new ArgumentNullException(nameof(securityRequirementChanges));
            if (yamlReader == null) throw new ArgumentNullException(nameof(yamlReader));
            if (driftOptions == null) throw new ArgumentNullException(nameof(driftOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            await ProcessAddedAsync(drift, securityRequirementChanges.AddedFilePaths, yamlReader, logger);
            await ProcessDeletedAsync(drift, securityRequirementChanges.DeletedFilePaths, yamlReader, logger);
            await ProcessModifiedAsync(drift, securityRequirementChanges.ModifiedFiles, yamlReader, driftOptions, logger);
        }

        // ─────────────────────────────────────────────────────────────
        // ADDED SECURITY REQUIREMENTS
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
                logger.LogInformation("No added security requirement files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} added security requirement files...", pathList.Count);

            var srs = await ReadSecurityRequirementsAsync(yamlReader, pathList, logger, "added");

            foreach (var sr in srs)
            {
                if (sr == null)
                {
                    logger.LogWarning("Encountered null SecurityRequirement while processing added security requirements.");
                    continue;
                }

                // 1) Try to hang it under AddedLibrary.SecurityRequirements
                var addedLib = drift.AddedLibraries
                    .FirstOrDefault(l => l.Library != null && l.Library.Guid == sr.LibraryId);

                if (addedLib != null)
                {
                    addedLib.SecurityRequirements.Add(sr);

                    logger.LogInformation(
                        "Added SecurityRequirement {SRGuid} attached to AddedLibrary {LibraryGuid}.",
                        sr.Guid,
                        sr.LibraryId);

                    continue;
                }

                // 2) Otherwise under LibraryDrift.SecurityRequirements.Added
                var libDrift = GetOrCreateLibraryDrift(drift, sr.LibraryId, logger);

                libDrift.SecurityRequirements.Added.Add(sr);

                logger.LogInformation(
                    "Added SecurityRequirement {SRGuid} attached to LibraryDrift.SecurityRequirements.Added for Library {LibraryGuid}.",
                    sr.Guid,
                    sr.LibraryId);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // DELETED SECURITY REQUIREMENTS
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
                logger.LogInformation("No deleted security requirement files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} deleted security requirement files...", pathList.Count);

            var srs = await ReadSecurityRequirementsAsync(yamlReader, pathList, logger, "deleted");

            foreach (var sr in srs)
            {
                if (sr == null)
                {
                    logger.LogWarning("Encountered null SecurityRequirement while processing deleted security requirements.");
                    continue;
                }

                // 1) Prefer DeletedLibrary.SecurityRequirements
                var deletedLib = drift.DeletedLibraries
                    .FirstOrDefault(l => l.LibraryGuid == sr.LibraryId);

                if (deletedLib != null)
                {
                    deletedLib.SecurityRequirements.Add(sr);

                    logger.LogInformation(
                        "Deleted SecurityRequirement {SRGuid} attached to DeletedLibrary {LibraryGuid}.",
                        sr.Guid,
                        sr.LibraryId);

                    continue;
                }

                // 2) Otherwise under LibraryDrift.SecurityRequirements.Removed
                var libDrift = GetOrCreateLibraryDrift(drift, sr.LibraryId, logger);

                libDrift.SecurityRequirements.Removed.Add(sr);

                logger.LogInformation(
                    "Deleted SecurityRequirement {SRGuid} attached to LibraryDrift.SecurityRequirements.Removed for Library {LibraryGuid}.",
                    sr.Guid,
                    sr.LibraryId);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // MODIFIED SECURITY REQUIREMENTS
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
                logger.LogInformation("No modified security requirement files detected.");
                return;
            }

            var pathList = modifiedPaths.Where(m => m != null).ToList();
            if (pathList.Count == 0)
            {
                logger.LogInformation("No modified security requirement files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} modified security requirement files...", pathList.Count);

            foreach (var modified in pathList)
            {
                SecurityRequirement? baseSR;
                SecurityRequirement? targetSR;

                try
                {
                    baseSR = await ReadSingleSecurityRequirementAsync(
                        yamlReader,
                        modified.BaseRepositoryFilePath,
                        logger);

                    targetSR = await ReadSingleSecurityRequirementAsync(
                        yamlReader,
                        modified.TargetRepositoryFilePath,
                        logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error reading SecurityRequirement YAML for modified file. Base={BasePath}, Target={TargetPath}",
                        modified.BaseRepositoryFilePath,
                        modified.TargetRepositoryFilePath);
                    throw;
                }

                if (baseSR == null || targetSR == null)
                {
                    logger.LogWarning(
                        "Unable to read both base and target SecurityRequirement for modified file. Base null={BaseNull}, Target null={TargetNull}",
                        baseSR == null,
                        targetSR == null);
                    continue;
                }

                // Compare only configured fields
                var changedFields = baseSR.CompareFields(
                    targetSR,
                    driftOptions.SecurityRequirementDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation(
                        "No changes detected in configured SecurityRequirement fields for {SRGuid}. Skipping modification.",
                        baseSR.Guid);
                    continue;
                }

                // Always belong to a modified library (create if needed)
                var libDrift = GetOrCreateLibraryDrift(drift, targetSR.LibraryId, logger);

                var modifiedEntity = new ModifiedEntity<SecurityRequirement>
                {
                    EntityKey = targetSR.Guid.ToString(),
                    EntityName = targetSR.Name,
                    ModifiedFields = changedFields,
                };

                libDrift.SecurityRequirements.Modified.Add(modifiedEntity);

                logger.LogInformation(
                    "Modified SecurityRequirement {SRGuid} attached to LibraryDrift.SecurityRequirements.Modified for Library {LibraryGuid}.",
                    targetSR.Guid,
                    targetSR.LibraryId);
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

        private static async Task<IReadOnlyCollection<SecurityRequirement>> ReadSecurityRequirementsAsync(
            IYamlReaderRouter yamlReader,
            IEnumerable<string> paths,
            ILogger logger,
            string operationName)
        {
            var pathList = NormalizePathList(paths);
            if (pathList.Count == 0)
            {
                return Array.Empty<SecurityRequirement>();
            }

            try
            {
                var result = await yamlReader.ReadSecurityRequirementsAsync(pathList);
                return result?.Where(sr => sr != null).ToList() ?? new List<SecurityRequirement>();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to read SecurityRequirement YAML files for operation '{Operation}'.",
                    operationName);
                throw;
            }
        }

        private static async Task<SecurityRequirement?> ReadSingleSecurityRequirementAsync(
            IYamlReaderRouter yamlReader,
            string path,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogWarning("ReadSingleSecurityRequirementAsync called with an empty path.");
                return null;
            }

            try
            {
                var list = await yamlReader.ReadSecurityRequirementsAsync(new[] { path });
                return list?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read SecurityRequirement YAML at path: {Path}", path);
                throw;
            }
        }

        /// <summary>
        /// Finds an existing LibraryDrift for a given libraryGuid, or creates one and
        /// adds it to TMFrameworkDrift.ModifiedLibraries.
        /// </summary>
        private static LibraryDrift GetOrCreateLibraryDrift(
            TMFrameworkDrift drift,
            Guid libraryGuid,
            ILogger logger)
        {
            var existing = drift.ModifiedLibraries
                .FirstOrDefault(ld => ld.LibraryGuid == libraryGuid);

            if (existing != null)
            {
                return existing;
            }

            var newDrift = new LibraryDrift
            {
                LibraryGuid = libraryGuid
            };

            drift.ModifiedLibraries.Add(newDrift);

            logger.LogInformation(
                "Created new LibraryDrift for LibraryGuid={LibraryGuid} to attach SecurityRequirement drift.",
                libraryGuid);

            return newDrift;
        }
    }
}

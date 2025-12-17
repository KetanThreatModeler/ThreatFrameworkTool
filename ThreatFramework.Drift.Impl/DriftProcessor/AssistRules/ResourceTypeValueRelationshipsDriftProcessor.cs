using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreatFramework.Core;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Model.AssistRules;
using ThreatModeler.TF.Core.Model.CoreEntities;
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

                // 1) Prefer AddedLibrary -> AddedResourceTypeValueDto.Relationships
                var addedLib = drift.AddedLibraries
                    .FirstOrDefault(l => l.Library != null && l.Library.Guid == rel.LibraryId);

                if (addedLib != null)
                {
                    var addedRtv = FindAddedRtv(addedLib, rel.SourceResourceTypeValue);
                    if (addedRtv != null)
                    {
                        addedRtv.Relationships.Add(rel);

                        logger.LogInformation(
                            "Added RTVR (SrcRTV={SourceRtv}, Rel={RelGuid}, TgtRTV={TargetRtv}) attached to AddedLibrary {LibraryGuid} -> AddedResourceTypeValue.Relationships.",
                            rel.SourceResourceTypeValue,
                            rel.RelationshipGuid,
                            rel.TargetResourceTypeValue,
                            rel.LibraryId);

                        continue;
                    }

                    logger.LogInformation(
                        "Added RTVR (SrcRTV={SourceRtv}, Rel={RelGuid}, TgtRTV={TargetRtv}) - AddedLibrary {LibraryGuid} found but Source RTV not found in AddedLibrary.ResourceTypeValues.",
                        rel.SourceResourceTypeValue,
                        rel.RelationshipGuid,
                        rel.TargetResourceTypeValue,
                        rel.LibraryId);
                    // Fall through to modified library handling below
                }

                // 2) Otherwise attach under ModifiedLibraries.ResourceTypeValues
                var libDrift = GetOrCreateLibraryDrift(drift, rel.LibraryId, logger);

                // 2a) Try in Added RTVs inside the modified lib drift (AddedResourceTypeValueDto.Relationships)
                var driftAddedRtv = FindAddedRtv(libDrift, rel.SourceResourceTypeValue);
                if (driftAddedRtv != null)
                {
                    driftAddedRtv.Relationships.Add(rel);

                    logger.LogInformation(
                        "Added RTVR (SrcRTV={SourceRtv}, Rel={RelGuid}, TgtRTV={TargetRtv}) attached to ModifiedLibrary {LibraryGuid} -> ResourceTypeValues.Added.Relationships.",
                        rel.SourceResourceTypeValue,
                        rel.RelationshipGuid,
                        rel.TargetResourceTypeValue,
                        rel.LibraryId);

                    continue;
                }

                // 2b) Try in Modified RTVs -> RelationshipsAdded
                var modifiedRtv = FindModifiedRtv(libDrift, rel.SourceResourceTypeValue);
                if (modifiedRtv != null)
                {
                    modifiedRtv.RelationshipsAdded.Add(rel);

                    logger.LogInformation(
                        "Added RTVR (SrcRTV={SourceRtv}, Rel={RelGuid}, TgtRTV={TargetRtv}) attached to ModifiedLibrary {LibraryGuid} -> ResourceTypeValues.Modified.RelationshipsAdded.",
                        rel.SourceResourceTypeValue,
                        rel.RelationshipGuid,
                        rel.TargetResourceTypeValue,
                        rel.LibraryId);

                    continue;
                }

                // 2c) If not found -> create new ModifiedResourceTypeValueDto and add to RelationshipsAdded
                var created = CreateModifiedRtvDtoStub(rel.SourceResourceTypeValue, logger);
                created.RelationshipsAdded.Add(rel);
                libDrift.ResourceTypeValues.Modified.Add(created);

                logger.LogInformation(
                    "Added RTVR (SrcRTV={SourceRtv}, Rel={RelGuid}, TgtRTV={TargetRtv}) created new ModifiedResourceTypeValueDto under Library {LibraryGuid} and attached to RelationshipsAdded.",
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

                // 1) Prefer DeletedLibrary -> RemovedResourceTypeValueDto.Relationships
                var deletedLib = drift.DeletedLibraries
                    .FirstOrDefault(l => l.LibraryGuid == rel.LibraryId);

                if (deletedLib != null)
                {
                    var removedRtv = FindRemovedRtv(deletedLib, rel.SourceResourceTypeValue);
                    if (removedRtv != null)
                    {
                        removedRtv.Relationships.Add(rel);

                        logger.LogInformation(
                            "Deleted RTVR (SrcRTV={SourceRtv}, Rel={RelGuid}, TgtRTV={TargetRtv}) attached to DeletedLibrary {LibraryGuid} -> RemovedResourceTypeValue.Relationships.",
                            rel.SourceResourceTypeValue,
                            rel.RelationshipGuid,
                            rel.TargetResourceTypeValue,
                            rel.LibraryId);

                        continue;
                    }

                    logger.LogInformation(
                        "Deleted RTVR (SrcRTV={SourceRtv}, Rel={RelGuid}, TgtRTV={TargetRtv}) - DeletedLibrary {LibraryGuid} found but Source RTV not found in DeletedLibrary.ResourceTypeValues.",
                        rel.SourceResourceTypeValue,
                        rel.RelationshipGuid,
                        rel.TargetResourceTypeValue,
                        rel.LibraryId);
                    // Fall through to modified library handling below
                }

                // 2) Otherwise attach under ModifiedLibraries.ResourceTypeValues
                var libDrift = GetOrCreateLibraryDrift(drift, rel.LibraryId, logger);

                // 2a) Try in Removed RTVs inside modified lib drift (RemovedResourceTypeValueDto.Relationships)
                var driftRemovedRtv = FindRemovedRtv(libDrift, rel.SourceResourceTypeValue);
                if (driftRemovedRtv != null)
                {
                    driftRemovedRtv.Relationships.Add(rel);

                    logger.LogInformation(
                        "Deleted RTVR (SrcRTV={SourceRtv}, Rel={RelGuid}, TgtRTV={TargetRtv}) attached to ModifiedLibrary {LibraryGuid} -> ResourceTypeValues.Removed.Relationships.",
                        rel.SourceResourceTypeValue,
                        rel.RelationshipGuid,
                        rel.TargetResourceTypeValue,
                        rel.LibraryId);

                    continue;
                }

                // 2b) Try in Modified RTVs -> RelationshipsRemoved
                var modifiedRtv = FindModifiedRtv(libDrift, rel.SourceResourceTypeValue);
                if (modifiedRtv != null)
                {
                    modifiedRtv.RelationshipsRemoved.Add(rel);

                    logger.LogInformation(
                        "Deleted RTVR (SrcRTV={SourceRtv}, Rel={RelGuid}, TgtRTV={TargetRtv}) attached to ModifiedLibrary {LibraryGuid} -> ResourceTypeValues.Modified.RelationshipsRemoved.",
                        rel.SourceResourceTypeValue,
                        rel.RelationshipGuid,
                        rel.TargetResourceTypeValue,
                        rel.LibraryId);

                    continue;
                }

                // 2c) If not found -> create new ModifiedResourceTypeValueDto and add to RelationshipsRemoved
                var created = CreateModifiedRtvDtoStub(rel.SourceResourceTypeValue, logger);
                created.RelationshipsRemoved.Add(rel);
                libDrift.ResourceTypeValues.Modified.Add(created);

                logger.LogInformation(
                    "Deleted RTVR (SrcRTV={SourceRtv}, Rel={RelGuid}, TgtRTV={TargetRtv}) created new ModifiedResourceTypeValueDto under Library {LibraryGuid} and attached to RelationshipsRemoved.",
                    rel.SourceResourceTypeValue,
                    rel.RelationshipGuid,
                    rel.TargetResourceTypeValue,
                    rel.LibraryId);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // MODIFIED (relationship changed)
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
                var changedFields = targetRel.CompareFields(
                    baseRel,
                    driftOptions.ResourceTypeValueRelationshipDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation(
                        "No changes detected in configured RTVR fields for (SrcRTV={SourceRtv}, Rel={RelGuid}, TgtRTV={TargetRtv}). Skipping modification.",
                        targetRel.SourceResourceTypeValue,
                        targetRel.RelationshipGuid,
                        targetRel.TargetResourceTypeValue);
                    continue;
                }

                // Find lib drift (modified relationship should land in ModifiedLibraries)
                var libDrift = GetOrCreateLibraryDrift(drift, targetRel.LibraryId, logger);

                // Find or create ModifiedResourceTypeValueDto for Source RTV
                var modifiedRtv = FindModifiedRtv(libDrift, targetRel.SourceResourceTypeValue);
                if (modifiedRtv == null)
                {
                    modifiedRtv = CreateModifiedRtvDtoStub(targetRel.SourceResourceTypeValue, logger);
                    libDrift.ResourceTypeValues.Modified.Add(modifiedRtv);

                    logger.LogInformation(
                        "Created new ModifiedResourceTypeValueDto for SourceRTV={SourceRtv} under LibraryGuid={LibraryGuid} to attach modified relationship.",
                        targetRel.SourceResourceTypeValue,
                        targetRel.LibraryId);
                }

                var modifiedRelDto = new ModifiedResourceTypeValueRelationshipDto
                {
                    Relationship = targetRel,
                    ChangedFields = changedFields
                };

                modifiedRtv.RelationshipsModified.Add(modifiedRelDto);

                logger.LogInformation(
                    "Modified RTVR (SrcRTV={SourceRtv}, Rel={RelGuid}, TgtRTV={TargetRtv}) attached to Library {LibraryGuid} -> ResourceTypeValues.Modified.RelationshipsModified.",
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

        // ---- ResourceTypeValue lookup helpers (AddedLibrary / DeletedLibrary) ----

        // AddedLibraryDto.ResourceTypeValues => List<AddedResourceTypeValueDto>
        private static AddedResourceTypeValueDto? FindAddedRtv(AddedLibraryDto lib, string sourceRtv)
        {
            return lib.ResourceTypeValues
                .FirstOrDefault(x => x?.ResourceTypeValue != null && x.ResourceTypeValue.ResourceTypeValue == sourceRtv);
        }

        // LibraryDriftDto.ResourceTypeValues.Added => List<AddedResourceTypeValueDto>
        private static AddedResourceTypeValueDto? FindAddedRtv(LibraryDriftDto libDrift, string sourceRtv)
        {
            return libDrift.ResourceTypeValues.Added
                .FirstOrDefault(x => x?.ResourceTypeValue != null && x.ResourceTypeValue.ResourceTypeValue == sourceRtv);
        }

        // DeletedLibraryDto.ResourceTypeValues => List<RemovedResourceTypeValueDto>
        private static RemovedResourceTypeValueDto? FindRemovedRtv(DeletedLibraryDto lib, string sourceRtv)
        {
            return lib.ResourceTypeValues
                .FirstOrDefault(x => x?.ResourceTypeValue != null && x.ResourceTypeValue.ResourceTypeValue == sourceRtv);
        }

        // LibraryDriftDto.ResourceTypeValues.Removed => List<RemovedResourceTypeValueDto>
        private static RemovedResourceTypeValueDto? FindRemovedRtv(LibraryDriftDto libDrift, string sourceRtv)
        {
            return libDrift.ResourceTypeValues.Removed
                .FirstOrDefault(x => x?.ResourceTypeValue != null && x.ResourceTypeValue.ResourceTypeValue == sourceRtv);
        }

        // LibraryDriftDto.ResourceTypeValues.Modified => List<ModifiedResourceTypeValueDto>
        private static ModifiedResourceTypeValueDto? FindModifiedRtv(LibraryDriftDto libDrift, string sourceRtv)
        {
            return libDrift.ResourceTypeValues.Modified
                .FirstOrDefault(x => x?.ResourceTypeValue != null && x.ResourceTypeValue.ResourceTypeValue == sourceRtv);
        }

        // Creates a stub RTV entity so ModifiedResourceTypeValueDto can exist even when RTV entity not loaded.
        // Replace this with a real "Fetch ResourceTypeValue" call if you have a yamlReader API for it.
        private static ModifiedResourceTypeValueDto CreateModifiedRtvDtoStub(string sourceRtv, ILogger logger)
        {
            logger.LogDebug(
                "Creating stub ResourceTypeValues for ResourceTypeValue={ResourceTypeValue}. Replace stub with a real fetch if available.",
                sourceRtv);

            var stub = new ResourceTypeValues
            {
                ResourceTypeValue = sourceRtv
            };

            return new ModifiedResourceTypeValueDto
            {
                ResourceTypeValue = stub
            };
        }
    }
}

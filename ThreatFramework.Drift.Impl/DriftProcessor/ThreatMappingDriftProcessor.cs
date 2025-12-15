using Microsoft.Extensions.Logging;
using ThreatFramework.Core;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Infra.Contract.Index;
using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor
{
    public static class ThreatMappingDriftProcessor
    {
        private sealed class ThreatMappingChangeBucket
        {
            public ThreatMappingChangeBucket(int threatId)
            {
                ThreatId = threatId;
            }

            public int ThreatId { get; }

            public ThreatMappingCollectionDto Added { get; } = new();
            public ThreatMappingCollectionDto Removed { get; } = new();
        }

        public static Task ProcessAsync(
            TMFrameworkDriftDto drift,
            IRepositoryDiffEntityPathContext pathContext,
            IGuidIndexService guidIndexService,
            IEnumerable<Guid> libraryIds,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (pathContext == null) throw new ArgumentNullException(nameof(pathContext));
            if (guidIndexService == null) throw new ArgumentNullException(nameof(guidIndexService));
            if (libraryIds == null) throw new ArgumentNullException(nameof(libraryIds));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            var buckets = BuildThreatMappingBuckets(pathContext, guidIndexService, logger);

            if (buckets.Count == 0)
            {
                logger.LogInformation("No threat mapping drift detected.");
                return Task.CompletedTask;
            }

            // 2) Attach mapping changes into TMFrameworkDrift
            var libraryIdList = libraryIds.Distinct().ToList();

            foreach (var bucket in buckets.Values)
            {
                AttachAddedMappingsForThreat(drift, bucket, guidIndexService, libraryIdList, logger);
                AttachRemovedMappingsForThreat(drift, bucket, guidIndexService, libraryIdList, logger);
            }

            return Task.CompletedTask;
        }

        // ─────────────────────────────────────────────────────────────
        // STEP 1: Build per-threat mapping buckets
        // ─────────────────────────────────────────────────────────────

        private static Dictionary<int, ThreatMappingChangeBucket> BuildThreatMappingBuckets(
            IRepositoryDiffEntityPathContext pathContext,
            IGuidIndexService guidIndexService,
            ILogger logger)
        {
            var buckets = new Dictionary<int, ThreatMappingChangeBucket>();

            var changeSet = pathContext.GetThreatSecurityRequirementsMappingFileChanges();

            AccumulateThreatMappingChangeSet(
                buckets,
                changeSet,
                guidIndexService,
                logger);


            logger.LogInformation("Created {Count} threat mapping buckets.", buckets.Count);

            return buckets;
        }

        private static void AccumulateThreatMappingChangeSet(
            Dictionary<int, ThreatMappingChangeBucket> buckets,
            EntityFileChangeSet changeSet,
            IGuidIndexService guidIndexService,
            ILogger logger)
        {
            if (changeSet == null)
            {
                return;
            }

            // Modified mappings are ignored for now (path-only diff semantics)
            if (changeSet.ModifiedFiles != null && changeSet.ModifiedFiles.Count > 0)
            {
                var modifiedPaths = changeSet.ModifiedFiles
                    .Where(m => m != null && !string.IsNullOrWhiteSpace(m.RelativePath))
                    .Select(m => m.RelativePath)
                    .ToList();

                if (modifiedPaths.Count > 0)
                {
                    logger.LogWarning(
                        "Threat mapping files with content modifications are currently ignored. Paths: {Paths}",
                        string.Join(", ", modifiedPaths));
                }
            }

            // Added mappings
            foreach (var path in changeSet.AddedFilePaths ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                try
                {
                    var ids = ParseIdsFromPath(path);
                    if (ids.Length < 2)
                        continue;

                    var threatId = ids[0];
                    var srId = ids[1];

                    var bucket = GetOrCreateThreatBucket(buckets, threatId);
                    AddThreatSRMappingToCollection(bucket.Added, threatId, srId, guidIndexService);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to parse/accumulate added threat mapping path: {Path}",
                        path);
                }
            }

            // Removed mappings
            foreach (var path in changeSet.DeletedFilePaths ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                try
                {
                    var ids = ParseIdsFromPath(path);
                    if (ids.Length < 2)
                        continue;

                    var threatId = ids[0];
                    var srId = ids[1];

                    var bucket = GetOrCreateThreatBucket(buckets, threatId);
                    AddThreatSRMappingToCollection(bucket.Removed, threatId, srId, guidIndexService);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to parse/accumulate deleted threat mapping path: {Path}",
                        path);
                }
            }
        }

        private static ThreatMappingChangeBucket GetOrCreateThreatBucket(
            IDictionary<int, ThreatMappingChangeBucket> buckets,
            int threatId)
        {
            if (!buckets.TryGetValue(threatId, out var bucket))
            {
                bucket = new ThreatMappingChangeBucket(threatId);
                buckets[threatId] = bucket;
            }

            return bucket;
        }

        /// <summary>
        /// Parse int IDs from a mapping path, e.g. "mappings/threat-sr/21_37.yaml" → [21, 37].
        /// </summary>
        private static int[] ParseIdsFromPath(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path); // "21_37"
            if (string.IsNullOrWhiteSpace(fileName))
                return Array.Empty<int>();

            var segments = fileName.Split('_', StringSplitOptions.RemoveEmptyEntries);
            var result = new List<int>();

            foreach (var segment in segments)
            {
                if (int.TryParse(segment, out var id))
                {
                    result.Add(id);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Adds a Threat ↔ SR mapping into the given ThreatMappingCollection.
        /// </summary>
        private static void AddThreatSRMappingToCollection(
            ThreatMappingCollectionDto target,
            int threatId,
            int srId,
            IGuidIndexService guidIndexService)
        {
            if (target == null)
            {
                return;
            }

            var srGuid = guidIndexService.GetGuid(srId);

            // If SRMappingDto has more fields (LibraryGuid, ThreatGuid, etc.), set them here as needed.
            target.SecurityRequirements.Add(
                new SRMappingDto
                {
                    SecurityRequirementId = srGuid
                });
        }

        // ─────────────────────────────────────────────────────────────
        // STEP 2: Attach added / removed mappings into TMFrameworkDrift
        // ─────────────────────────────────────────────────────────────

        private static void AttachAddedMappingsForThreat(
            TMFrameworkDriftDto drift,
            ThreatMappingChangeBucket bucket,
            IGuidIndexService guidIndexService,
            IReadOnlyCollection<Guid> libraryIds,
            ILogger logger)
        {
            if (!HasAnyThreatMappings(bucket.Added))
            {
                return;
            }

            var threatGuid = guidIndexService.GetGuid(bucket.ThreatId);

            // 1) Try AddedLibraries → AddedThreat.Mappings
            if (TryAttachToAddedLibraries(drift, threatGuid, bucket.Added, logger))
            {
                return;
            }

            // 2) Try ModifiedLibraries → Threats.Added[].Mappings
            if (TryAttachToModifiedLibrariesAddedThreats(drift, threatGuid, bucket.Added, logger))
            {
                return;
            }

            // 3) Try ModifiedLibraries → Threats.Modified[].MappingsAdded
            if (TryAttachToModifiedLibrariesModifiedThreats(drift, threatGuid, bucket.Added, isAdded: true, logger))
            {
                return;
            }

            // 4) Fallback – create a new LibraryDrift + ModifiedThreat
            var libraryGuid = ResolveLibraryGuidForThreat(bucket.ThreatId, libraryIds, guidIndexService, logger);
            var libDrift = GetOrCreateLibraryDrift(drift, libraryGuid, logger);

            EnsureThreatDrift(libDrift);

            var newModifiedThreat = new ModifiedThreatDto
            {
                Threat = new Threat
                {
                    Guid = threatGuid,
                    LibraryGuid = libraryGuid
                },
                ChangedFields = new List<FieldChange>(),
                MappingsAdded = new ThreatMappingCollectionDto(),
                MappingsRemoved = new ThreatMappingCollectionDto()
            };

            MergeThreatMappings(newModifiedThreat.MappingsAdded, bucket.Added);

            libDrift.Threats.Modified.Add(newModifiedThreat);

            logger.LogInformation(
                "Threat mapping (added) attached to NEW ModifiedThreat for ThreatId={ThreatId}, LibraryGuid={LibraryGuid}.",
                bucket.ThreatId,
                libraryGuid);
        }

        private static void AttachRemovedMappingsForThreat(
            TMFrameworkDriftDto drift,
            ThreatMappingChangeBucket bucket,
            IGuidIndexService guidIndexService,
            IReadOnlyCollection<Guid> libraryIds,
            ILogger logger)
        {
            if (!HasAnyThreatMappings(bucket.Removed))
            {
                return;
            }

            var threatGuid = guidIndexService.GetGuid(bucket.ThreatId);

            // 1) Try DeletedLibraries → RemovedThreat.Mappings
            if (TryAttachToDeletedLibraries(drift, threatGuid, bucket.Removed, logger))
            {
                return;
            }

            // 2) Try ModifiedLibraries → Threats.Removed[].Mappings
            if (TryAttachToModifiedLibrariesRemovedThreats(drift, threatGuid, bucket.Removed, logger))
            {
                return;
            }

            // 3) Try ModifiedLibraries → Threats.Modified[].MappingsRemoved
            if (TryAttachToModifiedLibrariesModifiedThreats(drift, threatGuid, bucket.Removed, isAdded: false, logger))
            {
                return;
            }

            // 4) Fallback – create a new LibraryDrift + ModifiedThreat
            var libraryGuid = ResolveLibraryGuidForThreat(bucket.ThreatId, libraryIds, guidIndexService, logger);
            var libDrift = GetOrCreateLibraryDrift(drift, libraryGuid, logger);

            EnsureThreatDrift(libDrift);

            var newModifiedThreat = new ModifiedThreatDto
            {
                Threat = new Threat
                {
                    Guid = threatGuid,
                    LibraryGuid = libraryGuid
                },
                ChangedFields = new List<FieldChange>(),
                MappingsAdded = new ThreatMappingCollectionDto(),
                MappingsRemoved = new ThreatMappingCollectionDto()
            };

            MergeThreatMappings(newModifiedThreat.MappingsRemoved, bucket.Removed);

            libDrift.Threats.Modified.Add(newModifiedThreat);

            logger.LogInformation(
                "Threat mapping (removed) attached to NEW ModifiedThreat for ThreatId={ThreatId}, LibraryGuid={LibraryGuid}.",
                bucket.ThreatId,
                libraryGuid);
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers to attach into existing drift structures
        // ─────────────────────────────────────────────────────────────

        private static bool TryAttachToAddedLibraries(
            TMFrameworkDriftDto drift,
            Guid threatGuid,
            ThreatMappingCollectionDto mappings,
            ILogger logger)
        {
            foreach (var addedLib in drift.AddedLibraries)
            {
                var threat = addedLib.Threats
                    .FirstOrDefault(t => t.Threat != null && t.Threat.Guid == threatGuid);

                if (threat == null)
                {
                    continue;
                }

                MergeThreatMappings(threat.Mappings, mappings);

                logger.LogInformation(
                    "Threat mappings (added) attached to AddedThreat in AddedLibrary {LibraryGuid}.",
                    addedLib.Library.Guid);

                return true;
            }

            return false;
        }

        private static bool TryAttachToDeletedLibraries(
            TMFrameworkDriftDto drift,
            Guid threatGuid,
            ThreatMappingCollectionDto mappings,
            ILogger logger)
        {
            foreach (var deletedLib in drift.DeletedLibraries)
            {
                var threat = deletedLib.Threats
                    .FirstOrDefault(t => t.Threat != null && t.Threat.Guid == threatGuid);

                if (threat == null)
                {
                    continue;
                }

                MergeThreatMappings(threat.Mappings, mappings);

                logger.LogInformation(
                    "Threat mappings (removed) attached to RemovedThreat in DeletedLibrary {LibraryGuid}.",
                    deletedLib.LibraryGuid);

                return true;
            }

            return false;
        }

        private static bool TryAttachToModifiedLibrariesAddedThreats(
            TMFrameworkDriftDto drift,
            Guid threatGuid,
            ThreatMappingCollectionDto mappings,
            ILogger logger)
        {
            foreach (var libDrift in drift.ModifiedLibraries)
            {
                if (libDrift.Threats?.Added == null) continue;

                var addedThreat = libDrift.Threats.Added
                    .FirstOrDefault(t => t.Threat != null && t.Threat.Guid == threatGuid);

                if (addedThreat == null)
                {
                    continue;
                }

                MergeThreatMappings(addedThreat.Mappings, mappings);

                logger.LogInformation(
                    "Threat mappings (added) attached to AddedThreat in ModifiedLibrary {LibraryGuid}.",
                    libDrift.LibraryGuid);

                return true;
            }

            return false;
        }

        private static bool TryAttachToModifiedLibrariesRemovedThreats(
            TMFrameworkDriftDto drift,
            Guid threatGuid,
            ThreatMappingCollectionDto mappings,
            ILogger logger)
        {
            foreach (var libDrift in drift.ModifiedLibraries)
            {
                if (libDrift.Threats?.Removed == null) continue;

                var removedThreat = libDrift.Threats.Removed
                    .FirstOrDefault(t => t.Threat != null && t.Threat.Guid == threatGuid);

                if (removedThreat == null)
                {
                    continue;
                }

                MergeThreatMappings(removedThreat.Mappings, mappings);

                logger.LogInformation(
                    "Threat mappings (removed) attached to RemovedThreat in ModifiedLibrary {LibraryGuid}.",
                    libDrift.LibraryGuid);

                return true;
            }

            return false;
        }

        private static bool TryAttachToModifiedLibrariesModifiedThreats(
            TMFrameworkDriftDto drift,
            Guid threatGuid,
            ThreatMappingCollectionDto mappings,
            bool isAdded,
            ILogger logger)
        {
            foreach (var libDrift in drift.ModifiedLibraries)
            {
                if (libDrift.Threats?.Modified == null) continue;

                var modifiedThreat = libDrift.Threats.Modified
                    .FirstOrDefault(t => t.Threat != null && t.Threat.Guid == threatGuid);

                if (modifiedThreat == null)
                {
                    continue;
                }

                if (isAdded)
                {
                    MergeThreatMappings(modifiedThreat.MappingsAdded, mappings);
                }
                else
                {
                    MergeThreatMappings(modifiedThreat.MappingsRemoved, mappings);
                }

                logger.LogInformation(
                    "Threat mappings ({Kind}) attached to ModifiedThreat in ModifiedLibrary {LibraryGuid}.",
                    isAdded ? "added" : "removed",
                    libDrift.LibraryGuid);

                return true;
            }

            return false;
        }

        private static void MergeThreatMappings(ThreatMappingCollectionDto target, ThreatMappingCollectionDto source)
        {
            if (target == null || source == null)
            {
                return;
            }

            target.SecurityRequirements.AddRange(source.SecurityRequirements);
        }

        private static bool HasAnyThreatMappings(ThreatMappingCollectionDto mapping)
        {
            if (mapping == null)
            {
                return false;
            }

            return mapping.SecurityRequirements.Count > 0;
        }

        private static Guid ResolveLibraryGuidForThreat(
            int threatId,
            IReadOnlyCollection<Guid> libraryIds,
            IGuidIndexService guidIndexService,
            ILogger logger)
        {
            // NOTE: This assumes IGuidIndexService can give you threat IDs per library.
            // Adjust to your actual index API (e.g. GetThreatIds, GetThreatIdsByLibrary, etc.).
            foreach (var libraryId in libraryIds)
            {
                var threatIds = guidIndexService.GetThreatIds(libraryId);
                if (threatIds != null && threatIds.Contains(threatId))
                {
                    return libraryId;
                }
            }

            logger.LogError(
                "Unable to resolve library GUID for threat ID {ThreatId}. Returning Guid.Empty.",
                threatId);

            return Guid.Empty;
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
                "Created new LibraryDrift for LibraryGuid={LibraryGuid} to attach threat mappings.",
                libraryGuid);

            return newDrift;
        }

        private static void EnsureThreatDrift(LibraryDriftDto libDrift)
        {
            if (libDrift.Threats == null)
            {
                libDrift.Threats = new ThreatDriftDto();
            }
        }
    }
}

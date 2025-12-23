using Microsoft.Extensions.Logging;
using ThreatFramework.Core;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Git.Contract.PathProcessor;
using ThreatModeler.TF.Infra.Contract.Index.TRC;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor.Mapping
{
    public static class ComponentMappingDriftProcessor
    {
        private enum ComponentMappingType
        {
            ComponentSecurityRequirement,
            ComponentThreat,
            ComponentThreatSecurityRequirement,
            ComponentProperty,
            ComponentPropertyOption,
            ComponentPropertyOptionThreat,
            ComponentPropertyOptionThreatSecurityRequirement
        }

        private sealed class ComponentMappingChangeBucket
        {
            public ComponentMappingChangeBucket(int componentId)
            {
                ComponentId = componentId;
            }

            public int ComponentId { get; }

            public ComponentMappingCollectionDto Added { get; } = new();
            public ComponentMappingCollectionDto Removed { get; } = new();
        }

        public static async Task ProcessAsync(
            TMFrameworkDriftDto drift,
            IRepositoryDiffEntityPathContext pathContext,
            ITRCGuidIndexService guidIndexService,
            IEnumerable<Guid> libraryIds,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (pathContext == null) throw new ArgumentNullException(nameof(pathContext));
            if (guidIndexService == null) throw new ArgumentNullException(nameof(guidIndexService));
            if (libraryIds == null) throw new ArgumentNullException(nameof(libraryIds));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            // 1) Group mappings per component (added / removed)
            var buckets = await BuildComponentMappingBucketsAsync(pathContext, guidIndexService, logger)
                .ConfigureAwait(false);

            if (buckets.Count == 0)
            {
                logger.LogInformation("No component mapping drift detected.");
                return;
            }

            // 2) Attach mapping changes into TMFrameworkDrift
            var libraryIdList = libraryIds.Distinct().ToList();

            foreach (var bucket in buckets.Values)
            {
                await AttachAddedMappingsForComponentAsync(drift, bucket, guidIndexService, libraryIdList, logger)
                    .ConfigureAwait(false);

                await AttachRemovedMappingsForComponentAsync(drift, bucket, guidIndexService, libraryIdList, logger)
                    .ConfigureAwait(false);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // STEP 1: Build per-component mapping buckets
        // ─────────────────────────────────────────────────────────────

        private static async Task<Dictionary<int, ComponentMappingChangeBucket>> BuildComponentMappingBucketsAsync(
            IRepositoryDiffEntityPathContext pathContext,
            ITRCGuidIndexService guidIndexService,
            ILogger logger)
        {
            var buckets = new Dictionary<int, ComponentMappingChangeBucket>();

            // For each mapping type, pull its change set and accumulate into shared buckets.
            await AccumulateMappingChangeSetAsync(
                buckets,
                pathContext.GetComponentSecurityRequirementsMappingFileChanges(),
                ComponentMappingType.ComponentSecurityRequirement,
                guidIndexService,
                logger).ConfigureAwait(false);

            await AccumulateMappingChangeSetAsync(
                buckets,
                pathContext.GetComponentThreatMappingFileChanges(),
                ComponentMappingType.ComponentThreat,
                guidIndexService,
                logger).ConfigureAwait(false);

            await AccumulateMappingChangeSetAsync(
                buckets,
                pathContext.GetComponentThreatSecurityRequirementsMappingFileChanges(),
                ComponentMappingType.ComponentThreatSecurityRequirement,
                guidIndexService,
                logger).ConfigureAwait(false);

            await AccumulateMappingChangeSetAsync(
                buckets,
                pathContext.GetComponentPropertyMappingFileChanges(),
                ComponentMappingType.ComponentProperty,
                guidIndexService,
                logger).ConfigureAwait(false);

            await AccumulateMappingChangeSetAsync(
                buckets,
                pathContext.GetComponentPropertyOptionsMappingFileChanges(),
                ComponentMappingType.ComponentPropertyOption,
                guidIndexService,
                logger).ConfigureAwait(false);

            await AccumulateMappingChangeSetAsync(
                buckets,
                pathContext.GetComponentPropertyOptionThreatsMappingFileChanges(),
                ComponentMappingType.ComponentPropertyOptionThreat,
                guidIndexService,
                logger).ConfigureAwait(false);

            await AccumulateMappingChangeSetAsync(
                buckets,
                pathContext.GetComponentPropertyOptionThreatSecurityRequirementsMappingFileChanges(),
                ComponentMappingType.ComponentPropertyOptionThreatSecurityRequirement,
                guidIndexService,
                logger).ConfigureAwait(false);

            return buckets;
        }

        private static async Task AccumulateMappingChangeSetAsync(
            Dictionary<int, ComponentMappingChangeBucket> buckets,
            EntityFileChangeSet changeSet,
            ComponentMappingType mappingType,
            ITRCGuidIndexService guidIndexService,
            ILogger logger)
        {
            if (changeSet == null)
                return;

            // Log modified mappings as warnings (ignored for now)
            if (changeSet.ModifiedFiles != null && changeSet.ModifiedFiles.Count > 0)
            {
                var modifiedPaths = changeSet.ModifiedFiles
                    .Where(m => m != null && !string.IsNullOrWhiteSpace(m.RelativePath))
                    .Select(m => m.RelativePath)
                    .ToList();

                if (modifiedPaths.Count > 0)
                {
                    logger.LogWarning(
                        "Component mapping files with content modifications are currently ignored. Paths: {Paths}",
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
                    if (ids.Length == 0)
                        continue;

                    var componentId = ids[0];
                    var bucket = GetOrCreateBucket(buckets, componentId);

                    await AddMappingToBucketAsync(bucket.Added, mappingType, ids, guidIndexService)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to parse/accumulate added component mapping path: {Path}",
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
                    if (ids.Length == 0)
                        continue;

                    var componentId = ids[0];
                    var bucket = GetOrCreateBucket(buckets, componentId);

                    await AddMappingToBucketAsync(bucket.Removed, mappingType, ids, guidIndexService)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to parse/accumulate deleted component mapping path: {Path}",
                        path);
                }
            }
        }

        private static ComponentMappingChangeBucket GetOrCreateBucket(
            IDictionary<int, ComponentMappingChangeBucket> buckets,
            int componentId)
        {
            if (!buckets.TryGetValue(componentId, out var bucket))
            {
                bucket = new ComponentMappingChangeBucket(componentId);
                buckets[componentId] = bucket;
            }

            return bucket;
        }

        /// <summary>
        /// Parse int IDs from a mapping path, e.g. "mappings/component-threat/21_27.yaml" → [21, 27].
        /// </summary>
        private static int[] ParseIdsFromPath(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path); // "21_27"
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
        /// Adds concrete mapping entries into the given ComponentMappingCollection,
        /// based on mapping type and numeric IDs (async Guid lookups).
        /// </summary>
        private static async Task AddMappingToBucketAsync(
            ComponentMappingCollectionDto target,
            ComponentMappingType mappingType,
            int[] ids,
            ITRCGuidIndexService guidIndexService)
        {
            if (ids == null || ids.Length == 0)
                return;

            // First ID is always the Component ID; others depend on mapping type
            switch (mappingType)
            {
                case ComponentMappingType.ComponentSecurityRequirement:
                    // compId_srId.yaml → [comp, sr]
                    if (ids.Length >= 2)
                    {
                        var srGuid = await guidIndexService.GetGuidAsync(ids[1]).ConfigureAwait(false);
                        target.SecurityRequirements.Add(new SRMappingDto { SecurityRequirementId = srGuid });
                    }
                    break;

                case ComponentMappingType.ComponentThreat:
                    // compId_threatId.yaml → [comp, threat]
                    if (ids.Length >= 2)
                    {
                        var threatGuid = await guidIndexService.GetGuidAsync(ids[1]).ConfigureAwait(false);
                        target.ThreatSRMappings.Add(new ThreatSRMappingDto(threatGuid));
                    }
                    break;

                case ComponentMappingType.ComponentThreatSecurityRequirement:
                    // comp_threat_sr.yaml → [comp, threat, sr]
                    if (ids.Length >= 3)
                    {
                        var threatGuid = await guidIndexService.GetGuidAsync(ids[1]).ConfigureAwait(false);
                        var srGuid = await guidIndexService.GetGuidAsync(ids[2]).ConfigureAwait(false);
                        target.ThreatSRMappings.Add(new ThreatSRMappingDto(threatGuid, srGuid));
                    }
                    break;

                case ComponentMappingType.ComponentProperty:
                    // comp_property.yaml → [comp, prop]
                    if (ids.Length >= 2)
                    {
                        var propGuid = await guidIndexService.GetGuidAsync(ids[1]).ConfigureAwait(false);
                        target.PropertyThreatSRMappings.Add(
                            new PropertyThreatSRMappingDto(propGuid, Guid.Empty, Guid.Empty, Guid.Empty));
                    }
                    break;

                case ComponentMappingType.ComponentPropertyOption:
                    // comp_prop_propOpt.yaml → [comp, prop, opt]
                    if (ids.Length >= 3)
                    {
                        var propGuid = await guidIndexService.GetGuidAsync(ids[1]).ConfigureAwait(false);
                        var optGuid = await guidIndexService.GetGuidAsync(ids[2]).ConfigureAwait(false);

                        target.PropertyThreatSRMappings.Add(
                            new PropertyThreatSRMappingDto(propGuid, optGuid, Guid.Empty, Guid.Empty));
                    }
                    break;

                case ComponentMappingType.ComponentPropertyOptionThreat:
                    // comp_prop_opt_threat.yaml → [comp, prop, opt, threat]
                    if (ids.Length >= 4)
                    {
                        var propGuid = await guidIndexService.GetGuidAsync(ids[1]).ConfigureAwait(false);
                        var optGuid = await guidIndexService.GetGuidAsync(ids[2]).ConfigureAwait(false);
                        var threatGuid = await guidIndexService.GetGuidAsync(ids[3]).ConfigureAwait(false);

                        target.PropertyThreatSRMappings.Add(
                            new PropertyThreatSRMappingDto(propGuid, optGuid, threatGuid, Guid.Empty));
                    }
                    break;

                case ComponentMappingType.ComponentPropertyOptionThreatSecurityRequirement:
                    // comp_prop_opt_threat_sr.yaml → [comp, prop, opt, threat, sr]
                    if (ids.Length >= 5)
                    {
                        var propGuid = await guidIndexService.GetGuidAsync(ids[1]).ConfigureAwait(false);
                        var optGuid = await guidIndexService.GetGuidAsync(ids[2]).ConfigureAwait(false);
                        var threatGuid = await guidIndexService.GetGuidAsync(ids[3]).ConfigureAwait(false);
                        var srGuid = await guidIndexService.GetGuidAsync(ids[4]).ConfigureAwait(false);

                        target.PropertyThreatSRMappings.Add(
                            new PropertyThreatSRMappingDto(propGuid, optGuid, threatGuid, srGuid));
                    }
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // STEP 2: Attach added / removed mappings into TMFrameworkDrift
        // ─────────────────────────────────────────────────────────────

        private static async Task AttachAddedMappingsForComponentAsync(
            TMFrameworkDriftDto drift,
            ComponentMappingChangeBucket bucket,
            ITRCGuidIndexService guidIndexService,
            IReadOnlyCollection<Guid> libraryIds,
            ILogger logger)
        {
            if (!HasAnyMappings(bucket.Added))
                return;

            var componentGuid = await guidIndexService.GetGuidAsync(bucket.ComponentId).ConfigureAwait(false);

            // 1) Try AddedLibraries → AddedComponent.Mappings
            if (TryAttachToAddedLibraries(drift, componentGuid, bucket.Added, logger))
                return;

            // 2) Try ModifiedLibraries → Components.Added[].Mappings
            if (TryAttachToModifiedLibrariesAddedComponents(drift, componentGuid, bucket.Added, logger))
                return;

            // 3) Try ModifiedLibraries → Components.Modified[].MappingsAdded
            if (TryAttachToModifiedLibrariesModifiedComponents(drift, componentGuid, bucket.Added, isAdded: true, logger))
                return;

            // 4) Fallback – create a new LibraryDrift + ModifiedComponent
            var libraryGuid = await ResolveLibraryGuidForComponentAsync(bucket.ComponentId, libraryIds, guidIndexService, logger)
                .ConfigureAwait(false);

            var libDrift = GetOrCreateLibraryDrift(drift, libraryGuid, logger);

            var newModifiedComponent = new ModifiedComponentDto
            {
                Component = new Component
                {
                    Guid = componentGuid,
                    LibraryGuid = libraryGuid
                },
                ChangedFields = new List<FieldChange>(),
                MappingsAdded = new ComponentMappingCollectionDto(),
                MappingsRemoved = new ComponentMappingCollectionDto()
            };

            MergeMappings(newModifiedComponent.MappingsAdded, bucket.Added);

            libDrift.Components.Modified.Add(newModifiedComponent);

            logger.LogInformation(
                "Component mapping (added) attached to NEW ModifiedComponent for ComponentId={ComponentId}, LibraryGuid={LibraryGuid}.",
                bucket.ComponentId,
                libraryGuid);
        }

        private static async Task AttachRemovedMappingsForComponentAsync(
            TMFrameworkDriftDto drift,
            ComponentMappingChangeBucket bucket,
            ITRCGuidIndexService guidIndexService,
            IReadOnlyCollection<Guid> libraryIds,
            ILogger logger)
        {
            if (!HasAnyMappings(bucket.Removed))
                return;

            var componentGuid = await guidIndexService.GetGuidAsync(bucket.ComponentId).ConfigureAwait(false);

            // 1) Try DeletedLibraries → DeletedComponent.Mappings
            if (TryAttachToDeletedLibraries(drift, componentGuid, bucket.Removed, logger))
                return;

            // 2) Try ModifiedLibraries → Components.Deleted[].Mappings
            if (TryAttachToModifiedLibrariesDeletedComponents(drift, componentGuid, bucket.Removed, logger))
                return;

            // 3) Try ModifiedLibraries → Components.Modified[].MappingsRemoved
            if (TryAttachToModifiedLibrariesModifiedComponents(drift, componentGuid, bucket.Removed, isAdded: false, logger))
                return;

            // 4) Fallback – create a new LibraryDrift + ModifiedComponent
            var libraryGuid = await ResolveLibraryGuidForComponentAsync(bucket.ComponentId, libraryIds, guidIndexService, logger)
                .ConfigureAwait(false);

            var libDrift = GetOrCreateLibraryDrift(drift, libraryGuid, logger);

            var newModifiedComponent = new ModifiedComponentDto
            {
                Component = new Component
                {
                    Guid = componentGuid,
                    LibraryGuid = libraryGuid
                },
                ChangedFields = new List<FieldChange>(),
                MappingsAdded = new ComponentMappingCollectionDto(),
                MappingsRemoved = new ComponentMappingCollectionDto()
            };

            MergeMappings(newModifiedComponent.MappingsRemoved, bucket.Removed);

            libDrift.Components.Modified.Add(newModifiedComponent);

            logger.LogInformation(
                "Component mapping (removed) attached to NEW ModifiedComponent for ComponentId={ComponentId}, LibraryGuid={LibraryGuid}.",
                bucket.ComponentId,
                libraryGuid);
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers to attach into existing drift structures (sync)
        // ─────────────────────────────────────────────────────────────

        private static bool TryAttachToAddedLibraries(
            TMFrameworkDriftDto drift,
            Guid componentGuid,
            ComponentMappingCollectionDto mappings,
            ILogger logger)
        {
            foreach (var addedLib in drift.AddedLibraries)
            {
                var comp = addedLib.Components
                    .FirstOrDefault(c => c.Component != null && c.Component.Guid == componentGuid);

                if (comp == null)
                    continue;

                MergeMappings(comp.Mappings, mappings);

                logger.LogInformation(
                    "Component mappings (added) attached to AddedComponent in AddedLibrary {LibraryGuid}.",
                    addedLib.Library.Guid);

                return true;
            }

            return false;
        }

        private static bool TryAttachToDeletedLibraries(
            TMFrameworkDriftDto drift,
            Guid componentGuid,
            ComponentMappingCollectionDto mappings,
            ILogger logger)
        {
            foreach (var deletedLib in drift.DeletedLibraries)
            {
                var comp = deletedLib.Components
                    .FirstOrDefault(c => c.Component != null && c.Component.Guid == componentGuid);

                if (comp == null)
                    continue;

                MergeMappings(comp.Mappings, mappings);

                logger.LogInformation(
                    "Component mappings (removed) attached to DeletedComponent in DeletedLibrary {LibraryGuid}.",
                    deletedLib.LibraryGuid);

                return true;
            }

            return false;
        }

        private static bool TryAttachToModifiedLibrariesAddedComponents(
            TMFrameworkDriftDto drift,
            Guid componentGuid,
            ComponentMappingCollectionDto mappings,
            ILogger logger)
        {
            foreach (var libDrift in drift.ModifiedLibraries)
            {
                var addedComp = libDrift.Components.Added
                    .FirstOrDefault(c => c.Component != null && c.Component.Guid == componentGuid);

                if (addedComp == null)
                    continue;

                MergeMappings(addedComp.Mappings, mappings);

                logger.LogInformation(
                    "Component mappings (added) attached to AddedComponent in ModifiedLibrary {LibraryGuid}.",
                    libDrift.LibraryGuid);

                return true;
            }

            return false;
        }

        private static bool TryAttachToModifiedLibrariesDeletedComponents(
            TMFrameworkDriftDto drift,
            Guid componentGuid,
            ComponentMappingCollectionDto mappings,
            ILogger logger)
        {
            foreach (var libDrift in drift.ModifiedLibraries)
            {
                var deletedComp = libDrift.Components.Deleted
                    .FirstOrDefault(c => c.Component != null && c.Component.Guid == componentGuid);

                if (deletedComp == null)
                    continue;

                MergeMappings(deletedComp.Mappings, mappings);

                logger.LogInformation(
                    "Component mappings (removed) attached to DeletedComponent in ModifiedLibrary {LibraryGuid}.",
                    libDrift.LibraryGuid);

                return true;
            }

            return false;
        }

        private static bool TryAttachToModifiedLibrariesModifiedComponents(
            TMFrameworkDriftDto drift,
            Guid componentGuid,
            ComponentMappingCollectionDto mappings,
            bool isAdded,
            ILogger logger)
        {
            foreach (var libDrift in drift.ModifiedLibraries)
            {
                var modifiedComp = libDrift.Components.Modified
                    .FirstOrDefault(c => c.Component != null && c.Component.Guid == componentGuid);

                if (modifiedComp == null)
                    continue;

                if (isAdded)
                    MergeMappings(modifiedComp.MappingsAdded, mappings);
                else
                    MergeMappings(modifiedComp.MappingsRemoved, mappings);

                logger.LogInformation(
                    "Component mappings ({Kind}) attached to ModifiedComponent in ModifiedLibrary {LibraryGuid}.",
                    isAdded ? "added" : "removed",
                    libDrift.LibraryGuid);

                return true;
            }

            return false;
        }

        private static void MergeMappings(ComponentMappingCollectionDto target, ComponentMappingCollectionDto source)
        {
            if (target == null || source == null)
                return;

            target.SecurityRequirements.AddRange(source.SecurityRequirements);
            target.ThreatSRMappings.AddRange(source.ThreatSRMappings);
            target.PropertyThreatSRMappings.AddRange(source.PropertyThreatSRMappings);
        }

        private static bool HasAnyMappings(ComponentMappingCollectionDto mapping)
        {
            if (mapping == null)
                return false;

            return mapping.SecurityRequirements.Count > 0
                   || mapping.ThreatSRMappings.Count > 0
                   || mapping.PropertyThreatSRMappings.Count > 0;
        }

        private static async Task<Guid> ResolveLibraryGuidForComponentAsync(
            int componentId,
            IReadOnlyCollection<Guid> libraryIds,
            ITRCGuidIndexService guidIndexService,
            ILogger logger)
        {
            foreach (var libraryId in libraryIds)
            {
                var componentIds = await guidIndexService.GetComponentIdsAsync(libraryId).ConfigureAwait(false);
                if (componentIds != null && componentIds.Contains(componentId))
                {
                    return libraryId;
                }
            }

            logger.LogWarning(
                "Unable to resolve library GUID for component ID {ComponentId}. Returning Guid.Empty.",
                componentId);

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
                "Created new LibraryDrift for LibraryGuid={LibraryGuid} to attach component mappings.",
                libraryGuid);

            return newDrift;
        }
    }
}

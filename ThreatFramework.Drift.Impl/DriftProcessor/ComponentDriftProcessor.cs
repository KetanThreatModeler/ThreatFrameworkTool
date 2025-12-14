using Microsoft.Extensions.Logging;
using ThreatFramework.Core;
using ThreatFramework.Core.CoreEntities;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor
{
    public static class ComponentDriftProcessor
    {
        public static async Task ProcessAsync(
            TMFrameworkDriftDto drift,
            EntityFileChangeSet componentChanges,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (componentChanges == null) throw new ArgumentNullException(nameof(componentChanges));
            if (yamlReader == null) throw new ArgumentNullException(nameof(yamlReader));
            if (driftOptions == null) throw new ArgumentNullException(nameof(driftOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            await ProcessAddedAsync(drift, componentChanges.AddedFilePaths, yamlReader, logger);
            await ProcessDeletedAsync(drift, componentChanges.DeletedFilePaths, yamlReader, logger);
            await ProcessModifiedAsync(drift, componentChanges.ModifiedFiles, yamlReader, driftOptions, logger);
        }

        // ─────────────────────────────────────────────────────────────
        // ADDED COMPONENTS
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
                logger.LogInformation("No added component files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} added component files...", pathList.Count);

            var components = await ReadComponentsAsync(yamlReader, pathList, logger, "added");

            foreach (var component in components)
            {
                if (component == null)
                {
                    logger.LogWarning("Encountered null Component while processing added components.");
                    continue;
                }

                var addedWrapper = new AddedComponentDto
                {
                    Component = component,
                    Mappings = new ComponentMappingCollectionDto()
                };

                // 1) Try to hang it under AddedLibrary.Components
                var addedLib = drift.AddedLibraries
                    .FirstOrDefault(l => l.Library != null && l.Library.Guid == component.LibraryGuid);

                if (addedLib != null)
                {
                    addedLib.Components.Add(addedWrapper);

                    logger.LogInformation(
                        "Added Component {ComponentGuid} attached to AddedLibrary {LibraryGuid}.",
                        component.Guid,
                        component.LibraryGuid);

                    continue;
                }

                // 2) Otherwise under LibraryDrift.Components.Added
                var libDrift = GetOrCreateLibraryDrift(drift, component.LibraryGuid, logger);

                libDrift.Components.Added.Add(addedWrapper);

                logger.LogInformation(
                    "Added Component {ComponentGuid} attached to LibraryDrift.Components.Added for Library {LibraryGuid}.",
                    component.Guid,
                    component.LibraryGuid);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // DELETED COMPONENTS
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
                logger.LogInformation("No deleted component files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} deleted component files...", pathList.Count);

            var components = await ReadComponentsAsync(yamlReader, pathList, logger, "deleted");

            foreach (var component in components)
            {
                if (component == null)
                {
                    logger.LogWarning("Encountered null Component while processing deleted components.");
                    continue;
                }

                var deletedWrapper = new DeletedComponentDto
                {
                    Component = component,
                    Mappings = new ComponentMappingCollectionDto()
                };

                // 1) Prefer DeletedLibrary.Components
                var deletedLib = drift.DeletedLibraries
                    .FirstOrDefault(l => l.LibraryGuid == component.LibraryGuid);

                if (deletedLib != null)
                {
                    deletedLib.Components.Add(deletedWrapper);

                    logger.LogInformation(
                        "Deleted Component {ComponentGuid} attached to DeletedLibrary {LibraryGuid}.",
                        component.Guid,
                        component.LibraryGuid);

                    continue;
                }

                // 2) Otherwise under LibraryDrift.Components.Deleted
                var libDrift = GetOrCreateLibraryDrift(drift, component.LibraryGuid, logger);

                libDrift.Components.Deleted.Add(deletedWrapper);

                logger.LogInformation(
                    "Deleted Component {ComponentGuid} attached to LibraryDrift.Components.Deleted for Library {LibraryGuid}.",
                    component.Guid,
                    component.LibraryGuid);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // MODIFIED COMPONENTS
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
                logger.LogInformation("No modified component files detected.");
                return;
            }

            var pathList = modifiedPaths.Where(m => m != null).ToList();
            if (pathList.Count == 0)
            {
                logger.LogInformation("No modified component files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} modified component files...", pathList.Count);

            foreach (var modified in pathList)
            {
                Component? baseComponent;
                Component? targetComponent;

                try
                {
                    baseComponent = await ReadSingleComponentAsync(
                        yamlReader,
                        modified.BaseRepositoryFilePath,
                        logger);

                    targetComponent = await ReadSingleComponentAsync(
                        yamlReader,
                        modified.TargetRepositoryFilePath,
                        logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error reading Component YAML for modified file. Base={BasePath}, Target={TargetPath}",
                        modified.BaseRepositoryFilePath,
                        modified.TargetRepositoryFilePath);
                    throw;
                }

                if (baseComponent == null || targetComponent == null)
                {
                    logger.LogWarning(
                        "Unable to read both base and target Component for modified file. Base null={BaseNull}, Target null={TargetNull}",
                        baseComponent == null,
                        targetComponent == null);
                    continue;
                }

                // Compare only configured fields
                var changedFields = baseComponent.CompareFields(
                    targetComponent,
                    driftOptions.ComponentDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation(
                        "No changes detected in configured Component fields for {ComponentGuid}. Skipping modification.",
                        baseComponent.Guid);
                    continue;
                }

                // Always belong to a modified library (create if needed)
                var libDrift = GetOrCreateLibraryDrift(drift, targetComponent.LibraryGuid, logger);

                var modifiedWrapper = new ModifiedComponentDto
                {
                    Component = targetComponent,
                    ChangedFields = changedFields,
                    MappingsAdded = new ComponentMappingCollectionDto(),
                    MappingsRemoved = new ComponentMappingCollectionDto()
                };

                libDrift.Components.Modified.Add(modifiedWrapper);

                logger.LogInformation(
                    "Modified Component {ComponentGuid} attached to LibraryDrift.Components.Modified for Library {LibraryGuid}.",
                    targetComponent.Guid,
                    targetComponent.LibraryGuid);
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

        private static async Task<IReadOnlyCollection<Component>> ReadComponentsAsync(
            IYamlReaderRouter yamlReader,
            IEnumerable<string> paths,
            ILogger logger,
            string operationName)
        {
            var pathList = NormalizePathList(paths);
            if (pathList.Count == 0)
            {
                return Array.Empty<Component>();
            }

            try
            {
                var result = await yamlReader.ReadComponentsAsync(pathList);
                return result?.Where(c => c != null).ToList() ?? new List<Component>();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to read Component YAML files for operation '{Operation}'.",
                    operationName);
                throw;
            }
        }

        private static async Task<Component?> ReadSingleComponentAsync(
            IYamlReaderRouter yamlReader,
            string path,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogWarning("ReadSingleComponentAsync called with an empty path.");
                return null;
            }

            try
            {
                var list = await yamlReader.ReadComponentsAsync(new[] { path });
                return list?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read Component YAML at path: {Path}", path);
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
                "Created new LibraryDrift for LibraryGuid={LibraryGuid} to attach Component drift.",
                libraryGuid);

            return newDrift;
        }
    }
}

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
using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation.DriftProcessor
{
    public static class TestCaseDriftProcessor
    {
        public static async Task ProcessAsync(
            TMFrameworkDriftDto drift,
            EntityFileChangeSet testCaseChanges,
            IYamlReaderRouter yamlReader,
            EntityDriftAggregationOptions driftOptions,
            ILogger logger)
        {
            if (drift == null) throw new ArgumentNullException(nameof(drift));
            if (testCaseChanges == null) throw new ArgumentNullException(nameof(testCaseChanges));
            if (yamlReader == null) throw new ArgumentNullException(nameof(yamlReader));
            if (driftOptions == null) throw new ArgumentNullException(nameof(driftOptions));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            await ProcessAddedAsync(drift, testCaseChanges.AddedFilePaths, yamlReader, logger);
            await ProcessDeletedAsync(drift, testCaseChanges.DeletedFilePaths, yamlReader, logger);
            await ProcessModifiedAsync(drift, testCaseChanges.ModifiedFiles, yamlReader, driftOptions, logger);
        }

        // ─────────────────────────────────────────────────────────────
        // ADDED TEST CASES
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
                logger.LogInformation("No added test case files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} added test case files...", pathList.Count);

            var testCases = await ReadTestCasesAsync(yamlReader, pathList, logger, "added");

            foreach (var testCase in testCases)
            {
                if (testCase == null)
                {
                    logger.LogWarning("Encountered null TestCase while processing added test cases.");
                    continue;
                }

                // 1) Try to hang it under AddedLibrary.TestCases
                var addedLib = drift.AddedLibraries
                    .FirstOrDefault(l => l.Library != null && l.Library.Guid == testCase.LibraryId);

                if (addedLib != null)
                {
                    addedLib.TestCases.Add(testCase);

                    logger.LogInformation(
                        "Added TestCase {TestGuid} attached to AddedLibrary {LibraryGuid}.",
                        testCase.Guid,
                        testCase.LibraryId);

                    continue;
                }

                // 2) Otherwise under LibraryDrift.TestCases.Added
                var libDrift = GetOrCreateLibraryDrift(drift, testCase.LibraryId, logger);

                libDrift.TestCases.Added.Add(testCase);

                logger.LogInformation(
                    "Added TestCase {TestGuid} attached to LibraryDrift.TestCases.Added for Library {LibraryGuid}.",
                    testCase.Guid,
                    testCase.LibraryId);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // DELETED TEST CASES
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
                logger.LogInformation("No deleted test case files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} deleted test case files...", pathList.Count);

            var testCases = await ReadTestCasesAsync(yamlReader, pathList, logger, "deleted");

            foreach (var testCase in testCases)
            {
                if (testCase == null)
                {
                    logger.LogWarning("Encountered null TestCase while processing deleted test cases.");
                    continue;
                }

                // 1) Prefer DeletedLibrary.TestCases
                var deletedLib = drift.DeletedLibraries
                    .FirstOrDefault(l => l.LibraryGuid == testCase.LibraryId);

                if (deletedLib != null)
                {
                    deletedLib.TestCases.Add(testCase);

                    logger.LogInformation(
                        "Deleted TestCase {TestGuid} attached to DeletedLibrary {LibraryGuid}.",
                        testCase.Guid,
                        testCase.LibraryId);

                    continue;
                }

                // 2) Otherwise under LibraryDrift.TestCases.Removed
                var libDrift = GetOrCreateLibraryDrift(drift, testCase.LibraryId, logger);

                libDrift.TestCases.Removed.Add(testCase);

                logger.LogInformation(
                    "Deleted TestCase {TestGuid} attached to LibraryDrift.TestCases.Removed for Library {LibraryGuid}.",
                    testCase.Guid,
                    testCase.LibraryId);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // MODIFIED TEST CASES
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
                logger.LogInformation("No modified test case files detected.");
                return;
            }

            var pathList = modifiedPaths.Where(m => m != null).ToList();
            if (pathList.Count == 0)
            {
                logger.LogInformation("No modified test case files detected.");
                return;
            }

            logger.LogInformation("Processing {Count} modified test case files...", pathList.Count);

            foreach (var modified in pathList)
            {
                TestCase? baseTestCase;
                TestCase? targetTestCase;

                try
                {
                    baseTestCase = await ReadSingleTestCaseAsync(
                        yamlReader,
                        modified.BaseRepositoryFilePath,
                        logger);

                    targetTestCase = await ReadSingleTestCaseAsync(
                        yamlReader,
                        modified.TargetRepositoryFilePath,
                        logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error reading TestCase YAML for modified file. Base={BasePath}, Target={TargetPath}",
                        modified.BaseRepositoryFilePath,
                        modified.TargetRepositoryFilePath);
                    throw;
                }

                if (baseTestCase == null || targetTestCase == null)
                {
                    logger.LogWarning(
                        "Unable to read both base and target TestCase for modified file. Base null={BaseNull}, Target null={TargetNull}",
                        baseTestCase == null,
                        targetTestCase == null);
                    continue;
                }

                // Compare only configured fields
                var changedFields = baseTestCase.CompareFields(
                    targetTestCase,
                    driftOptions.TestCaseDefaultFields);

                if (changedFields == null || changedFields.Count == 0)
                {
                    logger.LogInformation(
                        "No changes detected in configured TestCase fields for {TestGuid}. Skipping modification.",
                        baseTestCase.Guid);
                    continue;
                }

                // Always belong to a modified library (create if needed)
                var libDrift = GetOrCreateLibraryDrift(drift, targetTestCase.LibraryId, logger);

                var modifiedEntity = new ModifiedEntity<TestCase>
                {
                    Entity = baseTestCase,
                    ModifiedFields = changedFields,
                };

                libDrift.TestCases.Modified.Add(modifiedEntity);

                logger.LogInformation(
                    "Modified TestCase {TestGuid} attached to LibraryDrift.TestCases.Modified for Library {LibraryGuid}.",
                    targetTestCase.Guid,
                    targetTestCase.LibraryId);
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

        private static async Task<IReadOnlyCollection<TestCase>> ReadTestCasesAsync(
            IYamlReaderRouter yamlReader,
            IEnumerable<string> paths,
            ILogger logger,
            string operationName)
        {
            var pathList = NormalizePathList(paths);
            if (pathList.Count == 0)
            {
                return Array.Empty<TestCase>();
            }

            try
            {
                var result = await yamlReader.ReadTestCasesAsync(pathList);
                return result?.Where(tc => tc != null).ToList() ?? new List<TestCase>();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to read TestCase YAML files for operation '{Operation}'.",
                    operationName);
                throw;
            }
        }

        private static async Task<TestCase?> ReadSingleTestCaseAsync(
            IYamlReaderRouter yamlReader,
            string path,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogWarning("ReadSingleTestCaseAsync called with an empty path.");
                return null;
            }

            try
            {
                var list = await yamlReader.ReadTestCasesAsync(new[] { path });
                return list?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read TestCase YAML at path: {Path}", path);
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
                "Created new LibraryDrift for LibraryGuid={LibraryGuid} to attach TestCase drift.",
                libraryGuid);

            return newDrift;
        }
    }
}
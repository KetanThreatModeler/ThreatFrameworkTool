using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Git.Contract;
using ThreatModeler.TF.Git.Contract.Models;
using ThreatModeler.TF.Infra.Contract.Index.TRC;

namespace ThreatModeler.TF.Git.Implementation
{
    public sealed class LibraryScopedDiffService : ILibraryScopedDiffService
    {
        private readonly ITRCGuidIndexService _guidIndexService;
        private readonly IGitFolderDiffService _gitFolderDiffService;
        private readonly ILogger<LibraryScopedDiffService> _logger;

        public LibraryScopedDiffService(
            ITRCGuidIndexService guidIndexService,
            IGitFolderDiffService gitFolderDiffService,
            ILogger<LibraryScopedDiffService> logger)
        {
            _guidIndexService = guidIndexService ?? throw new ArgumentNullException(nameof(guidIndexService));
            _gitFolderDiffService = gitFolderDiffService ?? throw new ArgumentNullException(nameof(gitFolderDiffService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<FolderDiffReport> CompareLibrariesAsync(
            string baseRepositoryPath,
            string targetRepositoryPath,
            IEnumerable<Guid> libraryGuids,
            bool includeUncommittedChanges = true)
        {
            if (string.IsNullOrWhiteSpace(baseRepositoryPath))
                throw new ArgumentException("Base repository path is required.", nameof(baseRepositoryPath));

            if (string.IsNullOrWhiteSpace(targetRepositoryPath))
                throw new ArgumentException("Target repository path is required.", nameof(targetRepositoryPath));

            if (libraryGuids is null)
                throw new ArgumentNullException(nameof(libraryGuids));

            var libraryGuidList = libraryGuids.Distinct().ToList();
            if (!libraryGuidList.Any())
                throw new ArgumentException("At least one library GUID is required.", nameof(libraryGuids));

            using (_logger.BeginScope("Operation: CompareLibraries BaseRepo={BaseRepo} TargetRepo={TargetRepo} LibraryCount={Count}",
                                      baseRepositoryPath, targetRepositoryPath, libraryGuidList.Count))
            {
                try
                {
                    ValidateRepositoryPath(baseRepositoryPath, nameof(baseRepositoryPath));
                    ValidateRepositoryPath(targetRepositoryPath, nameof(targetRepositoryPath));

                    // 1. Resolve library folder names and collect entity IDs (components, threats, SRs).
                    var foldersToCheck = new List<string>();
                    var componentIds = new HashSet<int>();
                    var threatIds = new HashSet<int>();
                    var securityRequirementIds = new HashSet<int>();
                    foldersToCheck.Add("global");

                    foreach (var libraryGuid in libraryGuidList)
                    {
                        var libraryIntId = await _guidIndexService.GetIntAsync(libraryGuid);
                        var libraryFolder = FormatLibraryFolderName(libraryIntId);

                        foldersToCheck.Add(libraryFolder);

                        // Collect entity IDs for mapping-based comparisons.
                        AddRange(componentIds, await _guidIndexService.GetComponentIdsAsync(libraryGuid));
                        AddRange(threatIds, await _guidIndexService.GetThreatIdsAsync(libraryGuid));
                        AddRange(securityRequirementIds, await _guidIndexService.GetSecurityRequirementIdsAsync(libraryGuid));

                        _logger.LogDebug(
                            "Library {LibraryGuid} resolved to Id={LibraryId}, Folder={Folder}. Components={ComponentCount}, Threats={ThreatCount}, SRs={SrCount}.",
                            libraryGuid, libraryIntId, libraryFolder, componentIds.Count, threatIds.Count, securityRequirementIds.Count);
                    }

                    // 2. Compare the library folders themselves (01/, 06/, etc.)
                    var comparisonTasks = new List<Task<FolderDiffReport>>();

                    comparisonTasks.Add(_gitFolderDiffService.CompareFoldersAsync(
                        baseRepositoryPath,
                        targetRepositoryPath,
                        foldersToCheck,
                        includeUncommittedChanges));

                    // 3. Prepare mapping comparisons by prefixes.

                    var componentPrefixes = BuildIdPrefixes(componentIds);
                    var threatPrefixes = BuildIdPrefixes(threatIds);

                    // 3a. Component-based mapping folders.
                    if (componentPrefixes.Count > 0)
                    {
                        comparisonTasks.Add(CreatePrefixMatchingTask(
                            baseRepositoryPath,
                            targetRepositoryPath,
                            componentPrefixes,
                            includeUncommittedChanges));
                    }
                    else
                    {
                        _logger.LogInformation("No components found for given libraries. Skipping component-based mapping comparisons.");
                    }

                    // 3b. Threat-based mapping folder: threat-security-requirements
                    if (threatPrefixes.Count > 0)
                    {
                        comparisonTasks.Add(CreatePrefixMatchingTask(
                            baseRepositoryPath,
                            targetRepositoryPath,
                            threatPrefixes,
                            includeUncommittedChanges));
                    }
                    else
                    {
                        _logger.LogInformation("No threats found for given libraries. Skipping threat-based mapping comparisons.");
                    }

                    // 4. Execute all comparisons (in parallel).
                    var reports = await Task.WhenAll(comparisonTasks).ConfigureAwait(false);

                    // 5. Combine all FolderDiffReports into a single one.
                    var combinedReport = new FolderDiffReport();
                    foreach (var report in reports)
                    {
                        if (report is null) continue;
                        combinedReport.Merge(report);
                    }

                    Console.WriteLine(combinedReport.ToString());
                    _logger.LogInformation(
                        "Completed library-scoped comparison. Added={Added}, Deleted={Deleted}, Modified={Modified}.",
                        combinedReport.AddedPaths.Count,
                        combinedReport.DeletedPaths.Count,
                        combinedReport.ModifiedPaths.Count);

                    return combinedReport;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to compare libraries for BaseRepo={BaseRepo}, TargetRepo={TargetRepo}.",
                        baseRepositoryPath, targetRepositoryPath);
                    throw;
                }
            }
        }

        #region Helper Methods

        private static void ValidateRepositoryPath(string path, string paramName)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Repository path is required.", paramName);

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Repository path does not exist: {path}");
        }

        /// <summary>
        /// Formats the library folder name from its int ID, e.g. 1 -> "01/", 6 -> "06/", 34 -> "34/".
        /// </summary>
        private static string FormatLibraryFolderName(int libraryId)
        {
            return $"{libraryId}/";
        }

        /// <summary>
        /// Adds the contents of a sequence to a HashSet if the sequence is non-null.
        /// </summary>
        private static void AddRange(HashSet<int> target, IReadOnlyCollection<int> source)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) return;

            foreach (var value in source)
            {
                target.Add(value);
            }
        }

        /// <summary>
        /// Builds a list of prefixes like "21_", "30_" from numeric IDs.
        /// </summary>
        private static List<string> BuildIdPrefixes(IEnumerable<int> ids)
        {
            return ids?
                .Distinct()
                .OrderBy(id => id)
                .Select(id => id.ToString())
                .ToList()
                ?? new List<string>();
        }

        /// <summary>
        /// Creates CompareByPrefix tasks for all component-based mapping folders.
        /// </summary>
        private IEnumerable<Task<FolderDiffReport>> CreateComponentMappingTasks(
            string baseRepositoryPath,
            string targetRepositoryPath,
            List<string> componentPrefixes,
            bool includeUncommittedChanges)
        {
            if (componentPrefixes == null) throw new ArgumentNullException(nameof(componentPrefixes));

            // These folder paths are relative to repo root as per your structure.
            var mappingFolders = new[]
            {
                "mappings/component-property",
                "mappings/component-property-options",
                "mappings/component-property-option-threats",
                "mappings/component-property-option-threat-security-requirements",
                "mappings/component-threat",
                "mappings/component-threat-security-requirements",
                "mappings/component-security-requirements"
            };

            foreach (var folder in mappingFolders)
            {
                yield return _gitFolderDiffService.CompareByPrefixAsync(
                    baseRepositoryPath,
                    targetRepositoryPath,
                    folder,
                    componentPrefixes,
                    includeUncommittedChanges);
            }
        }

        /// <summary>
        /// Creates a CompareByPrefix task for the threat-based mapping folder: threat-security-requirements.
        /// </summary>
        private Task<FolderDiffReport> CreateThreatMappingTask(
            string baseRepositoryPath,
            string targetRepositoryPath,
            List<string> threatPrefixes,
            bool includeUncommittedChanges)
        {
            if (threatPrefixes == null) throw new ArgumentNullException(nameof(threatPrefixes));

            const string folder = "mappings/threat-security-requirements";

            return _gitFolderDiffService.CompareByPrefixAsync(
                baseRepositoryPath,
                targetRepositoryPath,
                folder,
                threatPrefixes,
                includeUncommittedChanges);
        }

        private Task<FolderDiffReport> CreatePrefixMatchingTask(
            string baseRepositoryPath,
            string targetRepositoryPath,
            List<string> threatPrefixes,
            bool includeUncommittedChanges)
        {
            if (threatPrefixes == null) throw new ArgumentNullException(nameof(threatPrefixes));

            const string folder = "mappings";

            return _gitFolderDiffService.CompareByPrefixAsync(
                baseRepositoryPath,
                targetRepositoryPath,
                folder,
                threatPrefixes,
                includeUncommittedChanges);
        }



        #endregion
    }
}

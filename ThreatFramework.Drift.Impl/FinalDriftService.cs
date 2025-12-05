using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using ThreatFramework.Core;
using ThreatFramework.Drift.Contract.CoreEntityDriftService;
using ThreatFramework.Drift.Contract.Model;
using ThreatFramework.Infra.Contract.Index;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatModeler.TF.Core.CoreEntities;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Drift.Implemenetation.DriftProcessor;
using ThreatModeler.TF.Git.Contract;
using ThreatModeler.TF.Git.Contract.Models;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Drift.Implemenetation
{
    public class FinalDriftService : IFinalDriftService
    {
        private readonly IGitService _gitService;
        private readonly IYamlFileGeneratorForClient _yamlFileGeneratorForClient;
        private readonly ILogger<FinalDriftService> _logger;
        private readonly GitSettings _gitSettings;
        private readonly PathOptions _pathOptions;
        private readonly ILibraryScopedDiffService _libraryScopedDiffService;
        private readonly IRepositoryDiffEntityPathService _repositoryDiffEntityPathService;
        private readonly IYamlReaderRouter _yamlReaderRouter;
        private readonly EntityDriftAggregationOptions _driftOptions;
        private readonly IGuidIndexService _guidIndexService;

        public FinalDriftService(
            IGitService gitService,
            IYamlFileGeneratorForClient yamlFileGeneratorForClient,
            IOptions<GitSettings> gitOptions,
            IOptions<PathOptions> pathOptions,
            ILibraryScopedDiffService libraryScopedDiffService,
            IYamlReaderRouter yamlReaderRouter,
            IRepositoryDiffEntityPathService repositoryDiffEntityPathService,
            IGuidIndexService guidIndexService,
            ILogger<FinalDriftService> logger)
        {
            _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            _yamlFileGeneratorForClient = yamlFileGeneratorForClient ?? throw new ArgumentNullException(nameof(yamlFileGeneratorForClient));
            _gitSettings = gitOptions?.Value ?? throw new ArgumentNullException(nameof(gitOptions));
            _pathOptions = pathOptions?.Value ?? throw new ArgumentNullException(nameof(pathOptions));
            _libraryScopedDiffService = libraryScopedDiffService ?? throw new ArgumentNullException(nameof(libraryScopedDiffService));
            _repositoryDiffEntityPathService = repositoryDiffEntityPathService ?? throw new ArgumentNullException(nameof(repositoryDiffEntityPathService));
            _yamlReaderRouter = yamlReaderRouter ?? throw new ArgumentNullException(nameof(yamlReaderRouter));
            _driftOptions = new EntityDriftAggregationOptions();
            _guidIndexService = guidIndexService ?? throw new ArgumentNullException(nameof(guidIndexService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TMFrameworkDrift> DriftAsync(IEnumerable<Guid> libraryIds, CancellationToken cancellationToken = default)
        {
            //1 sync the repo   
            _logger.LogInformation("Starting git sync...");
            //_gitService.SyncRepository(_gitSettings); // corrected to use _gitSettings
            _logger.LogInformation("Git sync completed.");

            //2 generate yaml files for the given library Ids
            _logger.LogInformation("Starting YAML file generation...");
            //await _yamlFileGeneratorForClient.GenerateForLibraryIdsAsync(_pathOptions.ClientOutput, libraryIds.ToList());
            _logger.LogInformation("YAML file generation completed.");


            _logger.LogInformation("Starting folder diff comparison...");
            FolderDiffReport folderDiffReport = await _libraryScopedDiffService.CompareLibrariesAsync(
                _pathOptions.TrcOutput,
                _pathOptions.ClientOutput,
                libraryIds,
                includeUncommittedChanges: true);
            _logger.LogInformation("Folder diff comparison completed.");


            _logger.LogInformation("Processing diff report to create TMFrameworkDrift...");
            IRepositoryDiffEntityPathContext ctx = _repositoryDiffEntityPathService.Create(folderDiffReport);
            _ = ctx.GetLibraryFileChanges();


            TMFrameworkDrift drift = new();
            await LibraryDriftProcessor.ProcessAsync(
                                        drift,
                                        ctx.GetLibraryFileChanges(),
                                        _yamlReaderRouter,
                                        _driftOptions,
                                        _logger
                                    );
            await TestCaseDriftProcessor.ProcessAsync(
                drift,
                ctx.GetTestCaseFileChanges(),
                _yamlReaderRouter,
                _driftOptions,
                _logger);


            await PropertyDriftProcessor.ProcessAsync(
                drift,
                ctx.GetPropertyFileChanges(),
                _yamlReaderRouter,
                _driftOptions,
                _logger);


            await SecurityRequirementDriftProcessor.ProcessAsync(
                drift,
                ctx.GetSecurityRequirementFileChanges(),
                _yamlReaderRouter,
                _driftOptions,
                _logger);

            await ThreatDriftProcessor.ProcessAsync(
                drift,
                ctx.GetThreatFileChanges(),
                _yamlReaderRouter,
                _driftOptions,
                _logger);

            await ComponentDriftProcessor.ProcessAsync(
                drift,
                ctx.GetComponentFileChanges(),
                _yamlReaderRouter,
                _driftOptions,
                _logger);

            await ComponentMappingDriftProcessor.ProcessAsync(
                drift,
                ctx,
                _guidIndexService,
                libraryIds,
                _logger);


            return drift;
        }
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using ThreatFramework.Infra.Contract.Repository;
using ThreatFramework.YamlFileGenerator.Contract; // Ensure this contains the updated IYamlFilesGeneratorForTRC interface
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Git.Contract;
using ThreatModeler.TF.Git.Contract.Models;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.TRC;
using ThreatModeler.TF.Infra.Contract.Index.TRC;
using ThreatModeler.TF.Infra.Contract.Repository;


namespace ThreatFramework.YamlFileGenerator.Impl
{
    public sealed class TrcYamlFilesGenerator : IYamlFilesGeneratorForTRC
    {
        private readonly ILogger<TrcYamlFilesGenerator> _logger;
        private readonly ILogger<YamlFilesGenerator> _yamlLogger;
        private readonly IRepositoryHub _hub;
        private readonly ITRCGuidIndexService _indexService;
        private readonly ITRCAssistRuleIndexService _assistRuleIndexQuery;
        private readonly ITRCAssistRuleIndexManager _assistRuleIndexManager;
        private readonly IGitService _gitService;
        private readonly GitSettings _gitSettings;
        private readonly PathOptions _options;

        public TrcYamlFilesGenerator(
            ILogger<TrcYamlFilesGenerator> logger,
            ILogger<YamlFilesGenerator> yamlLogger,
            IRepositoryHubFactory hubFactory,
            IOptions<PathOptions> options,
            IOptions<GitSettings> gitOptions,
            IGitService gitService,
            ITRCGuidIndexService indexService,
            ITRCAssistRuleIndexService assistRuleIndexQuery,
            ITRCAssistRuleIndexManager assistRuleIndexManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _yamlLogger = yamlLogger ?? throw new ArgumentNullException(nameof(yamlLogger));
            _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
            _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            _assistRuleIndexQuery = assistRuleIndexQuery ?? throw new ArgumentNullException(nameof(assistRuleIndexQuery));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _gitSettings = gitOptions?.Value ?? throw new ArgumentNullException(nameof(gitOptions));
            _assistRuleIndexManager = assistRuleIndexManager ?? throw new ArgumentNullException(nameof(assistRuleIndexManager));
            // Create the TRC-scoped hub
            _hub = hubFactory?.Create(DataPlane.Trc) ?? throw new ArgumentNullException(nameof(hubFactory));
        }

        public async Task GenerateForLibraryIdsAsync(string outputFolderPath, IEnumerable<Guid> libraryIds, bool pushToRemote = false)
        {
            using (_logger.BeginScope("Operation: GenerateForLibraryIds Path={Path} Push={Push}", outputFolderPath, pushToRemote))
            {
                var libList = libraryIds?.ToList() ?? new List<Guid>();
                if (!libList.Any())
                {
                    _logger.LogWarning("No library IDs provided. Skipping generation.");
                    return;
                }

                // 1. Generate Files
                await GenerateAsync(outputFolderPath, libList);

                // 2. Push if requested
                if (pushToRemote)
                {
                    HandleGitPush(outputFolderPath, $"Auto-Update: Generated YAMLs for {libList.Count} libraries.");
                }
            }
        }

        public async Task GenerateForReadOnlyLibraryAsync(string outputFolderPath, bool pushToRemote = false)
        {
            using (_logger.BeginScope("Operation: GenerateForReadOnlyLibrary Path={Path} Push={Push}", outputFolderPath, pushToRemote))
            {
                _logger.LogInformation("Fetching ReadOnly libraries from cache...");

                var libraryCaches = await _hub.Libraries.GetLibrariesCacheAsync();
                var readOnlyIds = libraryCaches.Where(x => x.IsReadonly).Select(x => x.Guid).ToList();

                if (!readOnlyIds.Any())
                {
                    _logger.LogWarning("No ReadOnly libraries found. Skipping generation.");
                    return;
                }

                // 1. Generate Files
                await GenerateAsync(outputFolderPath, readOnlyIds);

                // 2. Push if requested
                if (pushToRemote)
                {
                    HandleGitPush(outputFolderPath, "Auto-Update: Generated YAMLs for ReadOnly libraries.");
                }
            }
        }

        // --------------------------------------------------------------------------------
        // Core Logic (Private)
        // --------------------------------------------------------------------------------

        private async Task GenerateAsync(string outputFolderPath, List<Guid> libraryIds)
        {
            if (string.IsNullOrWhiteSpace(outputFolderPath))
                throw new ArgumentException("Output path is required.", nameof(outputFolderPath));

            if (_options.TrcOutput == null)
                throw new InvalidOperationException("YamlExport:Trc is not configured.");

            _logger.LogInformation("Starting TRC YAML export for {Count} libraries to {Root}.", libraryIds.Count, outputFolderPath);

            Directory.CreateDirectory(outputFolderPath);

            // 1. Initialize the internal worker
            var generator = CreateInternalGenerator();


            // 2. Generate Index File 
            await GenerateIndexAsync(outputFolderPath, libraryIds);

            // 3. Generate Entities
            await GenerateEntitiesAsync(generator, outputFolderPath, libraryIds);

            // 4. Generate Mappings
            await GenerateMappingsAsync(generator, outputFolderPath, libraryIds);

            _logger.LogInformation("TRC export completed successfully.");
        }

        private async Task GenerateIndexAsync(string root, List<Guid> libraryIds)
        {
            // Place index.yaml at the root level (same level as 'mappings' folder)
            var indexFilePath = Path.Combine(root, "index.yaml");

            _logger.LogInformation("Generating Index file at {Path}...", indexFilePath);

            // Call the service we created previously
            await _indexService.GenerateForLibraryAsync(libraryIds);

            _logger.LogInformation("Index file generated successfully.");

            await _assistRuleIndexManager.BuildAndWriteAsync(libraryIds);
        }

        // --------------------------------------------------------------------------------
        // Git Logic (Private Helper)
        // --------------------------------------------------------------------------------

        private void HandleGitPush(string outputFolderPath, string commitMessage)
        {
            // Safety Check: Ensure output folder is inside the configured Git Repo
            if (!IsPathInsideRepository(outputFolderPath, _gitSettings.LocalPath))
            {
                var ex = new InvalidOperationException(
                    $"Configuration Error: The output path '{outputFolderPath}' is not inside the configured Git Repository path '{_gitSettings.LocalPath}'. " +
                    "Files were generated but cannot be pushed.");

                _logger.LogError(ex, "Git Push aborted due to path mismatch.");
                throw ex;
            }

            _logger.LogInformation("Initiating Git Push...");

            var context = new GitCommitContext
            {
                // Connectivity
                RepoUrl = _gitSettings.RepoUrl,
                LocalPath = _gitSettings.LocalPath,
                Branch = _gitSettings.Branch,
                Username = _gitSettings.Username,
                Password = _gitSettings.Password,
                AuthorName = _gitSettings.AuthorName,
                AuthorEmail = _gitSettings.AuthorEmail,

                // Transactional
                CommitMessage = commitMessage
            };

            try
            {
                _gitService.CommitAndPush(context);
                _logger.LogInformation("Git Push completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to push changes to remote repository.");
                throw;
            }
        }

        private bool IsPathInsideRepository(string outputFolderPath, string repoRootPath)
        {
            var fullOutput = Path.GetFullPath(outputFolderPath);
            var fullRepo = Path.GetFullPath(repoRootPath);
            return fullOutput.StartsWith(fullRepo, StringComparison.OrdinalIgnoreCase);
        }

        // --------------------------------------------------------------------------------
        // Generator Orchestration (Private Helpers)
        // --------------------------------------------------------------------------------

        private async Task GenerateEntitiesAsync(YamlFilesGenerator gen, string root, List<Guid> libraryIds)
        {
            _logger.LogDebug("Generating Entity YAML files...");
            await gen.GenerateYamlFilesForSpecificLibraries(root, libraryIds);
            await gen.GenerateYamlFilesForSpecificComponents(root, libraryIds);
            await gen.GenerateYamlFilesForAllComponentTypes(root);
            await gen.GenerateYamlFilesForSpecificThreats(root, libraryIds);
            await gen.GenerateYamlFilesForSpecificSecurityRequirements(root, libraryIds);
            await gen.GenerateYamlFilesForSpecificProperties(root, libraryIds);
            await gen.GenerateYamlFilesForPropertyTypes(root);
            await gen.GenerateYamlFilesForSpecificTestCases(root, libraryIds);
            await gen.GenerateYamlFilesForPropertyOptions(root);
            //await gen.GenerateYamlFilesForRelationships(root);
            //await gen.GenerateYamlFilesForResourceTypeValues(root, libraryIds);
            //await gen.GenerateYamlFilesForResourceTypeValueRelationships(root, libraryIds);
        }

        private async Task GenerateMappingsAsync(YamlFilesGenerator gen, string root, List<Guid> libraryIds)
        {
            _logger.LogDebug("Generating Mapping YAML files...");
            var mappingsRoot = Path.Combine(root, "mappings");

            await gen.GenerateYamlFilesForComponentSecurityRequirementMappings(Path.Combine(mappingsRoot, "component-security-requirement"), libraryIds);
            await gen.GenerateYamlFilesForComponentThreatMappings(Path.Combine(mappingsRoot, "component-threat"), libraryIds);
            await gen.GenerateYamlFilesForComponentThreatSecurityRequirementMappings(Path.Combine(mappingsRoot, "component-threat-security-requirement"), libraryIds);
            await gen.GenerateYamlFilesForThreatSecurityRequirementMappings(Path.Combine(mappingsRoot, "threat-security-requirement"), libraryIds);
            await gen.GenerateYamlFilesForComponentPropertyMappings(Path.Combine(mappingsRoot, "component-property"), libraryIds);
            await gen.GenerateYamlFilesForComponentPropertyOptionMappings(Path.Combine(mappingsRoot, "component-property-option"), libraryIds);
            await gen.GenerateYamlFilesForComponentPropertyOptionThreatMappings(Path.Combine(mappingsRoot, "component-property-option-threat"), libraryIds);
            await gen.GenerateYamlFilesForComponentPropertyOptionThreatSecurityRequirementMappings(Path.Combine(mappingsRoot, "component-property-option-threat-security-requirement"), libraryIds);
        }

        private YamlFilesGenerator CreateInternalGenerator()
        {
            return new YamlFilesGenerator(
                logger: _yamlLogger,
                threatRepository: _hub.Threats,
                componentRepository: _hub.Components,
                componentTypeRepository: _hub.ComponentTypes,
                libraryRepository: _hub.Libraries,
                securityRequirementRepository: _hub.SecurityRequirements,
                propertyRepository: _hub.Properties,
                propertyTypeRepository: _hub.PropertyTypes,
                propertyOptionRepository: _hub.PropertyOptions,
                testcaseRepository: _hub.Testcases,
                componentThreatMappingRepository: _hub.ComponentThreatMappings,
                componentSecurityRequirementMappingRepository: _hub.ComponentSecurityRequirementMappings,
                componentThreatSecurityRequirementMappingRepository: _hub.ComponentThreatSecurityRequirementMappings,
                threatSecurityRequirementMappingRepository: _hub.ThreatSecurityRequirementMappings,
                componentPropertyMappingRepository: _hub.ComponentPropertyMappings,
                componentPropertyOptionMappingRepository: _hub.ComponentPropertyOptionMappings,
                componentPropertyOptionThreatMappingRepository: _hub.ComponentPropertyOptionThreatMappings,
                componentPropertyOptionThreatSecurityRequirementMappingRepository: _hub.ComponentPropertyOptionThreatSecurityRequirementMappings,
                indexService: _indexService,
                assistRuleIndexQuery: _assistRuleIndexQuery,
                relationshipRepository: _hub.Relationships,
                resourceTypeValuesRepository: _hub.ResourceTypeValues,
                resourceTypeValueRelationshipRepository: _hub.ResourceTypeValueRelationships
            );
        }
    }
}
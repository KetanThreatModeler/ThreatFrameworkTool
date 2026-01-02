using Microsoft.Extensions.Options;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Core.Model.Global;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Drift.Contract.Model;
using ThreatModeler.TF.Git.Contract.Common;
using ThreatModeler.TF.Infra.Contract.Index.Client;
using ThreatModeler.TF.Infra.Contract.Index.TRC;

namespace ThreatModeler.TF.Drift.Implemenetation
{
    public sealed class YamlRouter : IYamlRouter
    {
        private readonly ITRCGuidIndexService _trcIndexService;
        private readonly IClientGuidIndexService _clientIndexService;
        private readonly IYamlReaderRouter _yamlReaderRouter;
        private readonly PathOptions _paths;

        public YamlRouter(
            ITRCGuidIndexService trcIndexService,
            IClientGuidIndexService clientIndexService,
            IYamlReaderRouter yamlReaderRouter,
            IOptions<PathOptions> paths)
        {
            _trcIndexService = trcIndexService ?? throw new ArgumentNullException(nameof(trcIndexService));
            _clientIndexService = clientIndexService ?? throw new ArgumentNullException(nameof(clientIndexService));
            _yamlReaderRouter = yamlReaderRouter ?? throw new ArgumentNullException(nameof(yamlReaderRouter));
            _paths = paths?.Value ?? throw new ArgumentNullException(nameof(paths));
        }

        #region Public API

        public async Task<Library> GetLibraryByGuidAsync(Guid guid, DriftSource source)
        {
            EnsureGuid(guid);

            var libIntId = await GetIntIdAsync(source, guid).ConfigureAwait(false);
            var path = BuildLibraryPath(source, libIntId);

            var library = await _yamlReaderRouter
                .ReadLibraryAsync(path)
                .ConfigureAwait(false);

            return library ?? throw new InvalidOperationException(
                $"Library not found for guid {guid} at path '{path}'.");
        }

        public async Task<Component> GetComponentByGuidAsync(Guid guid, DriftSource source)
        {
            EnsureGuid(guid);

            var (entityId, libId) = await GetEntityAndLibIdsAsync(source, guid).ConfigureAwait(false);
            var path = BuildEntityPath(source, libId, FolderNames.Components, entityId);

            var component = await _yamlReaderRouter
                .ReadComponentAsync(path)
                .ConfigureAwait(false);

            return component ?? throw new InvalidOperationException(
                $"Component not found for guid {guid} at path '{path}'.");
        }

        public async Task<Threat> GetThreatByGuidAsync(Guid guid, DriftSource source)
        {
            EnsureGuid(guid);

            var (entityId, libId) = await GetEntityAndLibIdsAsync(source, guid).ConfigureAwait(false);
            var path = BuildEntityPath(source, libId, FolderNames.Threats, entityId);

            var threat = await _yamlReaderRouter
                .ReadThreatAsync(path)
                .ConfigureAwait(false);

            return threat ?? throw new InvalidOperationException(
                $"Threat not found for guid {guid} at path '{path}'.");
        }

        public async Task<SecurityRequirement> GetSecurityRequirementByGuidAsync(Guid guid, DriftSource source)
        {
            EnsureGuid(guid);

            var (entityId, libId) = await GetEntityAndLibIdsAsync(source, guid).ConfigureAwait(false);
            var path = BuildEntityPath(source, libId, FolderNames.SecurityRequirements, entityId);

            var sr = await _yamlReaderRouter
                .ReadSecurityRequirementAsync(path)
                .ConfigureAwait(false);

            return sr ?? throw new InvalidOperationException(
                $"Security requirement not found for guid {guid} at path '{path}'.");
        }

        public async Task<Property> GetPropertyByGuidAsync(Guid guid, DriftSource source)
        {
            EnsureGuid(guid);

            var (entityId, libId) = await GetEntityAndLibIdsAsync(source, guid).ConfigureAwait(false);
            var path = BuildEntityPath(source, libId, FolderNames.Properties, entityId);

            var property = await _yamlReaderRouter
                .ReadPropertyAsync(path)
                .ConfigureAwait(false);

            return property ?? throw new InvalidOperationException(
                $"Property not found for guid {guid} at path '{path}'.");
        }

        public async Task<TestCase> GetTestCaseByGuidAsync(Guid guid, DriftSource source)
        {
            EnsureGuid(guid);

            var (entityId, libId) = await GetEntityAndLibIdsAsync(source, guid).ConfigureAwait(false);
            var path = BuildEntityPath(source, libId, FolderNames.TestCases, entityId);

            var testCase = await _yamlReaderRouter
                .ReadTestCaseAsync(path)
                .ConfigureAwait(false);

            return testCase ?? throw new InvalidOperationException(
                $"Test case not found for guid {guid} at path '{path}'.");
        }

        public async Task<PropertyOption> GetPropertyOptionByGuidAsync(Guid guid, DriftSource source)
        {
            EnsureGuid(guid);

            var intId = await GetIntIdAsync(source, guid).ConfigureAwait(false);
            var path = BuildGlobalEntityPath(source, FolderNames.PropertyOptions, intId);

            var propertyOption = await _yamlReaderRouter
                .ReadPropertyOptionAsync(path)
                .ConfigureAwait(false);

            return propertyOption ?? throw new InvalidOperationException(
                $"PropertyOption not found for guid {guid} at path '{path}'.");
        }

        #endregion

        #region Helpers

        private static void EnsureGuid(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must not be empty.", nameof(guid));
        }

        /// <summary>
        /// Abstracts index service selection for "Guid -> int id" lookups.
        /// </summary>
        private async Task<int> GetIntIdAsync(DriftSource source, Guid guid) =>
            source switch
            {
                DriftSource.Client => await _clientIndexService.GetIntAsync(guid),
                DriftSource.GoldenDb => await _trcIndexService.GetIntAsync(guid),
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown DriftSource.")
            };

        /// <summary>
        /// Abstracts index service selection for "Guid -> (entityId, libId)" lookups.
        /// </summary>
        private async Task<(int entityId, int libId)> GetEntityAndLibIdsAsync(DriftSource source, Guid guid) =>
            source switch
            {
                DriftSource.Client => await _clientIndexService.GetIntIdOfEntityAndLibIdByGuidAsync(guid),
                DriftSource.GoldenDb => await  _trcIndexService.GetIntIdOfEntityAndLibIdByGuidAsync(guid),
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown DriftSource.")
            };

        private string GetBasePath(DriftSource source)
        {
            // If it's client then use ClientOutput otherwise use TrcOutput
            return source == DriftSource.Client
                ? _paths.ClientOutput
                : _paths.TrcOutput;
        }

        private string BuildLibraryPath(DriftSource source, int libraryIntId)
        {
            var basePath = GetBasePath(source);
            return Path.Combine(basePath, libraryIntId.ToString(), $"{libraryIntId}.yaml");
        }

        private string BuildEntityPath(DriftSource source, int libraryIntId, string folderName, int entityIntId)
        {
            var basePath = GetBasePath(source);
            return Path.Combine(basePath, libraryIntId.ToString(), folderName, $"{entityIntId}.yaml");
        }

        private string BuildGlobalEntityPath(DriftSource source, string folderName, int entityIntId)
        {
            var basePath = GetBasePath(source);
            return Path.Combine(basePath, "global", folderName, $"{entityIntId}.yaml");
        }

        #endregion
    }
}

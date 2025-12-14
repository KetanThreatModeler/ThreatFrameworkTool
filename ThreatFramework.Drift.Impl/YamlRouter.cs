using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract.CoreEntityDriftService;
using ThreatFramework.Infra.Contract.Index;
using ThreatModeler.TF.Core.CoreEntities;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Git.Contract.Common;

namespace ThreatModeler.TF.Drift.Implemenetation
{
    public sealed class YamlRouter : IYamlRouter
    {
        private readonly IGuidIndexService _indexService;
        private readonly IYamlReaderRouter _yamlReaderRouter;
        private readonly PathOptions _paths;

        public YamlRouter(
            IGuidIndexService indexService,
            IYamlReaderRouter yamlReaderRouter,
            IOptions<PathOptions> paths)
        {
            _indexService = indexService ?? throw new ArgumentNullException(nameof(indexService));
            _yamlReaderRouter = yamlReaderRouter ?? throw new ArgumentNullException(nameof(yamlReaderRouter));
            _paths = paths?.Value ?? throw new ArgumentNullException(nameof(paths));
        }

        #region Public API

        public async Task<Library> GetLibraryByGuidAsync(Guid guid, DriftSource source)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must not be empty.", nameof(guid));

            var libIntId = _indexService.GetInt(guid);
            var path = BuildLibraryPath(source, libIntId);

            var library = await _yamlReaderRouter
                .ReadLibraryAsync(path)
                .ConfigureAwait(false);

            if (library == null)
                throw new InvalidOperationException($"Library not found for guid {guid} at path '{path}'.");

            return library;
        }

        public async Task<Component> GetComponentByGuidAsync(Guid guid, DriftSource source)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must not be empty.", nameof(guid));

            var (entityId, libId) = await _indexService
                .GetIntIdOfEntityAndLibIdByGuidAsync(guid)
                .ConfigureAwait(false);

            var path = BuildEntityPath(source, libId, FolderNames.Components, entityId);

            var component = await _yamlReaderRouter
                .ReadComponentAsync(path)
                .ConfigureAwait(false);

            if (component == null)
                throw new InvalidOperationException($"Component not found for guid {guid} at path '{path}'.");

            return component;
        }

        public async Task<Threat> GetThreatByGuidAsync(Guid guid, DriftSource source)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must not be empty.", nameof(guid));

            var (entityId, libId) = await _indexService
                .GetIntIdOfEntityAndLibIdByGuidAsync(guid)
                .ConfigureAwait(false);

            var path = BuildEntityPath(source, libId, FolderNames.Threats, entityId);

            var threat = await _yamlReaderRouter
                .ReadThreatAsync(path)
                .ConfigureAwait(false);

            if (threat == null)
                throw new InvalidOperationException($"Threat not found for guid {guid} at path '{path}'.");

            return threat;
        }

        public async Task<SecurityRequirement> GetSecurityRequirementByGuidAsync(Guid guid, DriftSource source)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must not be empty.", nameof(guid));

            var (entityId, libId) = await _indexService
                .GetIntIdOfEntityAndLibIdByGuidAsync(guid)
                .ConfigureAwait(false);

            var path = BuildEntityPath(source, libId, FolderNames.SecurityRequirements, entityId);

            var sr = await _yamlReaderRouter
                .ReadSecurityRequirementAsync(path)
                .ConfigureAwait(false);

            if (sr == null)
                throw new InvalidOperationException(
                    $"Security requirement not found for guid {guid} at path '{path}'.");

            return sr;
        }

        public async Task<Property> GetPropertyByGuidAsync(Guid guid, DriftSource source)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must not be empty.", nameof(guid));

            var (entityId, libId) = await _indexService
                .GetIntIdOfEntityAndLibIdByGuidAsync(guid)
                .ConfigureAwait(false);

            var path = BuildEntityPath(source, libId, FolderNames.Properties, entityId);

            var property = await _yamlReaderRouter
                .ReadPropertyAsync(path)
                .ConfigureAwait(false);

            if (property == null)
                throw new InvalidOperationException($"Property not found for guid {guid} at path '{path}'.");

            return property;
        }

        public async Task<TestCase> GetTestCaseByGuidAsync(Guid guid, DriftSource source)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must not be empty.", nameof(guid));

            var (entityId, libId) = await _indexService
                .GetIntIdOfEntityAndLibIdByGuidAsync(guid)
                .ConfigureAwait(false);

            var path = BuildEntityPath(source, libId, FolderNames.TestCases, entityId);

            var testCase = await _yamlReaderRouter
                .ReadTestCaseAsync(path)
                .ConfigureAwait(false);

            if (testCase == null)
                throw new InvalidOperationException($"Test case not found for guid {guid} at path '{path}'.");

            return testCase;
        }

        public async Task<PropertyOption> GetPropertyOptionByGuidAsync(Guid guid, DriftSource source)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("Guid must not be empty.", nameof(guid));

            var intId = _indexService.GetInt(guid);
            var path = BuildGlobalEntityPath(source, FolderNames.PropertyOptions, intId);

            var propertyOption = await _yamlReaderRouter
                .ReadPropertyOptionAsync(path)
                .ConfigureAwait(false);

            if (propertyOption == null)
                throw new InvalidOperationException($"PropertyOption not found for guid {guid} at path '{path}'.");

            return propertyOption;
        }

        #endregion

        #region Helpers

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
            return Path.Combine(basePath, "global",folderName, $"{entityIntId}.yaml");
        }

        #endregion
    }
}

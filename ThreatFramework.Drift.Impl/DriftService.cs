using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract;
using ThreatFramework.Drift.Contract.Model;
using ThreatFramework.Infra.Contract;

namespace ThreatFramework.Drift.Impl
{
    public class DriftService : IDriftService
    {
        private readonly IEntityRepository<Threat> _threatRepo;
        private readonly IEntityRepository<Component> _componentRepo;
        private readonly IEntityRepository<SecurityRequirement> _secReqRepo;
        private readonly IEntityRepository<TestCase> _testCaseRepo;
        private readonly IEntityRepository<Property> _propertyRepo;
        private readonly IEntityRepository<PropertyOption> _propOptionRepo;
        private readonly IEntityRepository<Library> _libraryRepo;
        private readonly IDiffEngine<Threat> _threatDiff;
        private readonly IDiffEngine<Component> _componentDiff;
        private readonly IDiffEngine<SecurityRequirement> _secReqDiff;
        private readonly IDiffEngine<TestCase> _testCaseDiff;
        private readonly IDiffEngine<Property> _propertyDiff;
        private readonly IDiffEngine<PropertyOption> _propOptionDiff;
        private readonly IIdentityResolver _idResolver;

        public DriftService(
            IEntityRepository<Threat> threatRepo,
            IEntityRepository<Component> componentRepo,
            IEntityRepository<SecurityRequirement> secReqRepo,
            IEntityRepository<TestCase> testCaseRepo,
            IEntityRepository<Property> propertyRepo,
            IEntityRepository<PropertyOption> propOptionRepo,
            IEntityRepository<Library> libraryRepo,
            IDiffEngine<Threat> threatDiff,
            IDiffEngine<Component> componentDiff,
            IDiffEngine<SecurityRequirement> secReqDiff,
            IDiffEngine<TestCase> testCaseDiff,
            IDiffEngine<Property> propertyDiff,
            IDiffEngine<PropertyOption> propOptionDiff,
            IIdentityResolver idResolver)
        {
            _threatRepo = threatRepo;
            _componentRepo = componentRepo;
            _secReqRepo = secReqRepo;
            _testCaseRepo = testCaseRepo;
            _propertyRepo = propertyRepo;
            _propOptionRepo = propOptionRepo;
            _libraryRepo = libraryRepo;
            _threatDiff = threatDiff;
            _componentDiff = componentDiff;
            _secReqDiff = secReqDiff;
            _testCaseDiff = testCaseDiff;
            _propertyDiff = propertyDiff;
            _propOptionDiff = propOptionDiff;
            _idResolver = idResolver;
        }

        public async Task<DriftAnalyzeResponse> AnalyzeAsync(DriftAnalyzeRequest request, CancellationToken ct = default)
        {
            // Validate input paths
            // Repositories will throw if invalid; we rely on that for simplicity

            // Optimization: if a DiffSummary list is provided, pass relevant paths to repositories
            IEnumerable<string>? added = request.DriftSummaryResponse?.AddedFiles;
            IEnumerable<string>? removed = request.DriftSummaryResponse?.RemovedFiles;
            IEnumerable<string>? modified = request.DriftSummaryResponse?.ModifiedFiles;
            IEnumerable<string>? changed = null;
            if (added != null || removed != null || modified != null)
            {
                changed = new List<string>(((added ?? Array.Empty<string>())
                    .Concat(removed ?? Array.Empty<string>())
                    .Concat(modified ?? Array.Empty<string>()))
                    .Distinct(StringComparer.OrdinalIgnoreCase));
            }

            // Load all entity sets from baseline and target
            var baseLibraries = await _libraryRepo.LoadAllAsync(request.BaselineFolderPath, changed, ct);
            var targLibraries = await _libraryRepo.LoadAllAsync(request.TargetFolderPath, changed, ct);

            var libNameByGuid = targLibraries.ToDictionary(l => l.Guid, l => l.Name);
            foreach (var l in baseLibraries)
                libNameByGuid.TryAdd(l.Guid, l.Name);

            // Load other entities
            var baseThreats = await _threatRepo.LoadAllAsync(request.BaselineFolderPath, changed, ct);
            var targThreats = await _threatRepo.LoadAllAsync(request.TargetFolderPath, changed, ct);

            var baseComponents = await _componentRepo.LoadAllAsync(request.BaselineFolderPath, changed, ct);
            var targComponents = await _componentRepo.LoadAllAsync(request.TargetFolderPath, changed, ct);

            var baseSecReqs = await _secReqRepo.LoadAllAsync(request.BaselineFolderPath, changed, ct);
            var targSecReqs = await _secReqRepo.LoadAllAsync(request.TargetFolderPath, changed, ct);

            var baseTestCases = await _testCaseRepo.LoadAllAsync(request.BaselineFolderPath, changed, ct);
            var targTestCases = await _testCaseRepo.LoadAllAsync(request.TargetFolderPath, changed, ct);

            var baseProps = await _propertyRepo.LoadAllAsync(request.BaselineFolderPath, changed, ct);
            var targProps = await _propertyRepo.LoadAllAsync(request.TargetFolderPath, changed, ct);

            var basePropOpts = await _propOptionRepo.LoadAllAsync(request.BaselineFolderPath, changed, ct);
            var targPropOpts = await _propOptionRepo.LoadAllAsync(request.TargetFolderPath, changed, ct);

            // Optional filters
            Func<string, bool>? includeFieldPredicate = request.IncludeFields is { Count: > 0 }
                ? new Func<string, bool>(f => request.IncludeFields!.Contains(f, StringComparer.OrdinalIgnoreCase))
                : null;

            var response = new DriftAnalyzeResponse();

            // Determine all library GUIDs present in either side or referenced by entities
            var libraryGuids = new HashSet<Guid>(targLibraries.Select(l => l.Guid).Concat(baseLibraries.Select(l => l.Guid)));
            foreach (var g in baseThreats.Select(_idResolver.GetLibraryGuid).Concat(targThreats.Select(_idResolver.GetLibraryGuid))) if (g.HasValue) libraryGuids.Add(g.Value);
            foreach (var g in baseComponents.Select(_idResolver.GetLibraryGuid).Concat(targComponents.Select(_idResolver.GetLibraryGuid))) if (g.HasValue) libraryGuids.Add(g.Value);
            foreach (var g in baseSecReqs.Select(_idResolver.GetLibraryGuid).Concat(targSecReqs.Select(_idResolver.GetLibraryGuid))) if (g.HasValue) libraryGuids.Add(g.Value);
            foreach (var g in baseTestCases.Select(_idResolver.GetLibraryGuid).Concat(targTestCases.Select(_idResolver.GetLibraryGuid))) if (g.HasValue) libraryGuids.Add(g.Value);
            foreach (var g in baseProps.Select(_idResolver.GetLibraryGuid).Concat(targProps.Select(_idResolver.GetLibraryGuid))) if (g.HasValue) libraryGuids.Add(g.Value);

            // Apply optional library filter
            if (request.IncludeLibraries is { Count: > 0 })
                libraryGuids.RemoveWhere(g => !request.IncludeLibraries!.Contains(g));

            // Apply optional entity filter helper
            bool Include(string entityName) => request.IncludeEntities is null || request.IncludeEntities.Count == 0 || request.IncludeEntities.Contains(entityName, StringComparer.OrdinalIgnoreCase);

            foreach (var libGuid in libraryGuids)
            {
                var libName = libNameByGuid.TryGetValue(libGuid, out var name) ? name : libGuid.ToString();

                var libThreatsBase = baseThreats.Where(t => _idResolver.GetLibraryGuid(t) == libGuid).ToList();
                var libThreatsTarg = targThreats.Where(t => _idResolver.GetLibraryGuid(t) == libGuid).ToList();

                var libComponentsBase = baseComponents.Where(t => _idResolver.GetLibraryGuid(t) == libGuid).ToList();
                var libComponentsTarg = targComponents.Where(t => _idResolver.GetLibraryGuid(t) == libGuid).ToList();

                var libSecReqsBase = baseSecReqs.Where(t => _idResolver.GetLibraryGuid(t) == libGuid).ToList();
                var libSecReqsTarg = targSecReqs.Where(t => _idResolver.GetLibraryGuid(t) == libGuid).ToList();

                var libTestCasesBase = baseTestCases.Where(t => _idResolver.GetLibraryGuid(t) == libGuid).ToList();
                var libTestCasesTarg = targTestCases.Where(t => _idResolver.GetLibraryGuid(t) == libGuid).ToList();

                var libPropsBase = baseProps.Where(t => _idResolver.GetLibraryGuid(t) == libGuid).ToList();
                var libPropsTarg = targProps.Where(t => _idResolver.GetLibraryGuid(t) == libGuid).ToList();

                var libDrift = new LibraryDrift
                {
                    LibraryGuid = libGuid,
                    LibraryName = libName,
                    Threats = Include("Threat") ? _threatDiff.Diff(libThreatsBase, libThreatsTarg, includeFieldPredicate) : new EntityDiff<Threat>(),
                    Components = Include("Component") ? _componentDiff.Diff(libComponentsBase, libComponentsTarg, includeFieldPredicate) : new EntityDiff<Component>(),
                    SecurityRequirements = Include("SecurityRequirement") ? _secReqDiff.Diff(libSecReqsBase, libSecReqsTarg, includeFieldPredicate) : new EntityDiff<SecurityRequirement>(),
                    TestCases = Include("TestCase") ? _testCaseDiff.Diff(libTestCasesBase, libTestCasesTarg, includeFieldPredicate) : new EntityDiff<TestCase>(),
                    Properties = Include("Property") ? _propertyDiff.Diff(libPropsBase, libPropsTarg, includeFieldPredicate) : new EntityDiff<Property>()
                };

                // Skip empty libraries if nothing changed (optional). Keep it by default to show explicit no-change.
                response.Libraries.Add(libDrift);
            }

            // Global part (PropertyOption only)
            if (Include("PropertyOption"))
            {
                response.Global = new GlobalDrift
                {
                    PropertyOptions = _propOptionDiff.Diff(basePropOpts, targPropOpts, includeFieldPredicate)
                };
            }

            return response;
        }
    }
}

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Common.Model;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.TRC;
using ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Common;

namespace ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Service
{
    public sealed class TRCAssistRuleIndexService : ITRCAssistRuleIndexService
    {
        private const string IndexKey = "AssistRuleIndex::Entries";

        private readonly ITRCAssistRuleIndexManager _manager;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TRCAssistRuleIndexService> _log;
        private readonly string _indexPath;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new();

        public TRCAssistRuleIndexService(
            ITRCAssistRuleIndexManager manager,
            IMemoryCache cache,
            IOptions<PathOptions> options,
            ILogger<TRCAssistRuleIndexService> log)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _log = log ?? throw new ArgumentNullException(nameof(log));

            _indexPath = options?.Value?.AssistRuleIndexYaml ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_indexPath))
                throw new ArgumentException("Configuration error: 'AssistRuleIndexYaml' is missing in PathOptions.", nameof(options));
        }

        // --------------------------------------------------------------------
        // Public API
        // --------------------------------------------------------------------

        public async Task GenerateIndexAsync()
        {
            using (_log.BeginScope("Operation: AssistRuleIndex.GenerateIndex"))
            {
                var gate = GetGate();
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    _log.LogInformation("Generating AssistRule index (global) and writing to yaml.");

                    var entries = await _manager.BuildAndWriteAsync().ConfigureAwait(false);
                    SetCache(entries);

                    _log.LogInformation("AssistRule index generated and cached. Count={Count}", entries.Count);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to generate AssistRule index (global).");
                    throw;
                }
                finally
                {
                    gate.Release();
                }
            }
        }

        public async Task GenerateIndexAsync(IEnumerable<Guid> libraryGuids)
        {
            if (libraryGuids == null) throw new ArgumentNullException(nameof(libraryGuids));

            var libs = libraryGuids.Where(g => g != Guid.Empty).Distinct().ToList();
            if (libs.Count == 0)
                throw new ArgumentException("At least one non-empty library guid is required.", nameof(libraryGuids));

            using (_log.BeginScope("Operation: AssistRuleIndex.GenerateIndexForLibraries"))
            {
                var gate = GetGate();
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    _log.LogInformation("Generating AssistRule index for {LibCount} libraries and writing to yaml.", libs.Count);

                    var entries = await _manager.BuildAndWriteAsync(libs).ConfigureAwait(false);
                    SetCache(entries);

                    _log.LogInformation("AssistRule index generated for libraries and cached. Count={Count}", entries.Count);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to generate AssistRule index for libraries.");
                    throw;
                }
                finally
                {
                    gate.Release();
                }
            }
        }

        public async Task RefreshAsync()
        {
            using (_log.BeginScope("Operation: AssistRuleIndex.Refresh"))
            {
                var gate = GetGate();
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    _log.LogInformation("Reloading AssistRule index from yaml.");

                    var entries = await _manager.ReloadFromYamlAsync().ConfigureAwait(false);
                    SetCache(entries);

                    _log.LogInformation("AssistRule index refreshed and cached. Count={Count}", entries.Count);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to refresh AssistRule index from yaml.");
                    throw;
                }
                finally
                {
                    gate.Release();
                }
            }
        }

        public async Task<int> GetIdByRelationshipGuidAsync(Guid relationshipGuid)
        {
            if (relationshipGuid == Guid.Empty)
                throw new ArgumentException("RelationshipGuid must be a non-empty value.", nameof(relationshipGuid));

            var entries = await EnsureLoadedAsync().ConfigureAwait(false);

            var identity = relationshipGuid.ToString();
            var match = entries.FirstOrDefault(e =>
                e.Type == AssistRuleType.Relationship &&
                string.Equals(e.Identity, identity, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                _log.LogError("RelationshipGuid {Guid} not found in AssistRule index cache.", relationshipGuid);
                throw new KeyNotFoundException($"RelationshipGuid {relationshipGuid} not found in AssistRule index.");
            }

            return match.Id;
        }

        public async Task<int> GetIdByResourceTypeValueAsync(string resourceTypeValue)
        {
            if (string.IsNullOrWhiteSpace(resourceTypeValue))
                throw new ArgumentException("ResourceTypeValue is required.", nameof(resourceTypeValue));

            var entries = await EnsureLoadedAsync().ConfigureAwait(false);

            var match = entries.FirstOrDefault(e =>
                e.Type == AssistRuleType.ResourceTypeValues &&
                string.Equals(e.Identity, ResourceTypeValueNormalizer.Normalize(resourceTypeValue), StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                _log.LogError("ResourceTypeValue '{Value}' not found in AssistRule index cache.", resourceTypeValue);
                throw new KeyNotFoundException($"ResourceTypeValue '{resourceTypeValue}' not found in AssistRule index.");
            }

            return match.Id;
        }

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> GetResourceTypeValuesByLibraryGuidAsync(Guid libraryGuid)
        {
            if (libraryGuid == Guid.Empty)
                throw new ArgumentException("LibraryGuid must be a non-empty value.", nameof(libraryGuid));

            var entries = await EnsureLoadedAsync().ConfigureAwait(false);

            var list = entries
                .Where(e => e.Type == AssistRuleType.ResourceTypeValues && e.LibraryGuid == libraryGuid)
                .ToList();

            if (list.Count == 0)
            {
                _log.LogError("No ResourceTypeValues found for LibraryGuid={LibraryGuid} in AssistRule index.", libraryGuid);
                throw new KeyNotFoundException($"No ResourceTypeValues found for LibraryGuid={libraryGuid} in AssistRule index.");
            }

            return list.AsReadOnly();
        }

        public async Task<IReadOnlyList<AssistRuleIndexEntry>> GetAllAsync()
        {
            var entries = await EnsureLoadedAsync().ConfigureAwait(false);
            return entries.AsReadOnly();
        }

        public async Task<int> GetMaxAssignedIdAsync()
        {
            using (_log.BeginScope("Operation: AssistRuleIndex.GetMaxAssignedIdAsync"))
            {
                var entries = await EnsureLoadedAsync().ConfigureAwait(false);

                if (entries.Count == 0)
                    return 0;

                return entries
                    .Select(e => e.Id)
                    .Max();
            }
        }

        private SemaphoreSlim GetGate()
            => Gates.GetOrAdd(_indexPath, _ => new SemaphoreSlim(1, 1));

        private void SetCache(IReadOnlyList<AssistRuleIndexEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            _cache.Set(IndexKey, entries.ToList());
        }

        private bool TryGetCache(out List<AssistRuleIndexEntry> entries)
        {
            if (_cache.TryGetValue(IndexKey, out List<AssistRuleIndexEntry>? cached) && cached != null)
            {
                entries = cached;
                return true;
            }

            entries = null;
            return false;
        }

        private async Task<List<AssistRuleIndexEntry>> EnsureLoadedAsync()
        {
            if (TryGetCache(out var cached))
                return cached;

            var gate = GetGate();
            await gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (TryGetCache(out cached))
                    return cached;

                _log.LogInformation("AssistRule index cache empty. Reloading from yaml.");
                var entries = await _manager.ReloadFromYamlAsync().ConfigureAwait(false);

                var list = entries?.ToList() ?? new List<AssistRuleIndexEntry>();
                _cache.Set(IndexKey, list);

                return list;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to ensure AssistRule index loaded (cache empty and reload failed).");
                throw;
            }
            finally
            {
                gate.Release();
            }
        }
    }
}

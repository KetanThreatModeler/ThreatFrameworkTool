using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Model;
using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Service;

namespace ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Service
{
    public sealed class AssistRuleIndexCache : IAssistRuleIndexQuery
    {
        private readonly object _sync = new();

        private List<AssistRuleIndexEntry> _all = new();

        private readonly ConcurrentDictionary<string, string> _relationshipIdentityToId =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, string> _rtvIdentityToId =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<Guid, List<AssistRuleIndexEntry>> _rtvByLibrary =
            new();

        public void ReplaceAll(IReadOnlyList<AssistRuleIndexEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            lock (_sync)
            {
                _all = entries.ToList();

                _relationshipIdentityToId.Clear();
                _rtvIdentityToId.Clear();
                _rtvByLibrary.Clear();

                foreach (var e in _all)
                {
                    if (e.Type == AssistRuleType.Relationship)
                    {
                        _relationshipIdentityToId[e.Identity] = e.Id;
                    }
                    else if (e.Type == AssistRuleType.ResourceTypeValues)
                    {
                        _rtvIdentityToId[e.Identity] = e.Id;

                        _rtvByLibrary.AddOrUpdate(
                            e.LibraryGuid,
                            _ => new List<AssistRuleIndexEntry> { e },
                            (_, list) =>
                            {
                                list.Add(e);
                                return list;
                            });
                    }
                }
            }
        }

        public bool TryGetIdByRelationshipGuid(Guid relationshipGuid, out string id)
            => _relationshipIdentityToId.TryGetValue(relationshipGuid.ToString(), out id);

        public bool TryGetIdByResourceTypeValue(string resourceTypeValue, out string id)
        {
            id = null;
            if (string.IsNullOrWhiteSpace(resourceTypeValue)) return false;
            return _rtvIdentityToId.TryGetValue(resourceTypeValue, out id);
        }

        public IReadOnlyList<AssistRuleIndexEntry> GetResourceTypeValuesByLibraryGuid(Guid libraryGuid)
        {
            if (_rtvByLibrary.TryGetValue(libraryGuid, out var list))
                return list.AsReadOnly();

            return Array.Empty<AssistRuleIndexEntry>();
        }

        public IReadOnlyList<AssistRuleIndexEntry> GetAll() => _all.AsReadOnly();
    }
}

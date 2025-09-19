using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Core;
using ThreatFramework.Drift.Contract.Model;

namespace ThreatFramework.Drift.Contract
{
    public interface ILibraryDriftAggregator
    {
        /// <summary>
        /// Aggregates a repo-level EntityDriftReport into per-library drift with field-sensitive modified entries.
        /// </summary>
        Task<IReadOnlyList<LibraryDrift>> AggregateAsync(
            EntityDriftReport report,
            EntityDriftAggregationOptions? options = null,
            // Optional per-call field overrides:
            IEnumerable<string>? libraryFields = null,
            IEnumerable<string>? threatFields = null,
            IEnumerable<string>? componentFields = null,
            IEnumerable<string>? securityRequirementFields = null,
            IEnumerable<string>? testCaseFields = null,
            IEnumerable<string>? propertyFields = null,
            CancellationToken cancellationToken = default);
    }

}

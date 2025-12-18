using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Drift.Contract.Dto
{
    public sealed class LibraryChangeSummaryDto
    {
        public string LibraryName { get; init; } = string.Empty;
        public Guid LibraryGuid { get; init; }

        public string Operation { get; init; } = string.Empty;

        public string? ExistingVersion { get; init; }
        public string? NewVersion { get; init; }

        public string? ReleaseNote { get; init; }
    }
}

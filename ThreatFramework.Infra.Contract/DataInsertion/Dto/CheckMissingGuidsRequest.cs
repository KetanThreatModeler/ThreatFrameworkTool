using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract.DataInsertion.Dto
{
    public sealed class CheckMissingGuidsRequest
    {
        public IReadOnlyCollection<Guid> ThreatIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyCollection<Guid> SecurityRequirementIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyCollection<Guid> PropertyIds { get; init; } = Array.Empty<Guid>();
    }
}

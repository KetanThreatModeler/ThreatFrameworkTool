using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract.DataInsertion.Dto
{
    public sealed class MissingGuidsByEntity
    {
        public HashSet<Guid> Threats { get; } = new();
        public HashSet<Guid> SecurityRequirements { get; } = new();
        public HashSet<Guid> Properties { get; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract.Index
{
    public class EntityIdentifier
    {
        public Guid Guid { get; set; }
        public Guid LibraryGuid { get; set; }
        public EntityType EntityType { get; set; }
    }

    public class GuidIndex : EntityIdentifier
    {
        public int Id { get; set; }
    }
}

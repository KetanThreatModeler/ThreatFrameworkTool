using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Core.Model.AssistRules
{
    public class ResourceTypeValueRelationship
    {
        public string SourceResourceTypeValue { get; set; }

        public Guid RelationshipGuid { get; set; }

        public string TargetResourceTypeValue { get; set; }

        public bool IsRequired { get; set; }

        public Guid LibraryId { get; set; }

        public bool IsDeleted { get; set; }
    }
}

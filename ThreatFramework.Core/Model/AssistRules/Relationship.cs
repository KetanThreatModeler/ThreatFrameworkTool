using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Core.Model.AssistRules
{
    public class Relationship
    {
        public string RelationshipName { get; set; }

        public string Description { get; set; }

        public Guid Guid { get; set; }

        public string ChineseRelationship { get; set; }
    }
}

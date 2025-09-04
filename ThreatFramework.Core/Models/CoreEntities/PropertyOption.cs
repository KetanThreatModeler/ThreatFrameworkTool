using System;
using System.Collections.Generic;

namespace ThreatFramework.Core.Models.CoreEntities
{

    public class PropertyOption
    {
        public int Id { get; set; }
        public int? PropertyId { get; set; }
        public bool IsDefault { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public Guid? Guid { get; set; }
        public string OptionText { get; set; }
        public string? ChineseOptionText { get; set; }
    }
}

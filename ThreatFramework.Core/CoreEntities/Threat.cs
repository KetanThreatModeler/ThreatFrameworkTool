namespace ThreatFramework.Core.CoreEntities
{
    public class Threat : IFieldComparable<Threat>
    {
        public int Id { get; set; }
        public int RiskId { get; set; }
        public Guid LibraryGuid { get; set; }
        public bool Automated { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public Guid Guid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ChineseName { get; set; }
        public string? Labels { get; set; }
        public string? Description { get; set; }
        public string? Reference { get; set; }
        public string? Intelligence { get; set; }
        public string? ChineseDescription { get; set; }

        public IReadOnlyList<FieldChange> CompareFields(Threat other, IEnumerable<string> fields)
      => FieldComparer.CompareByNames(this, other, fields);
    }
}

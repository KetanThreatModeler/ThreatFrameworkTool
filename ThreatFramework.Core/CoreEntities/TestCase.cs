namespace ThreatFramework.Core.CoreEntities
{
    public class TestCase : IFieldComparable<TestCase>
    {
        public int Id { get; set; }
        public Guid LibraryId { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string? ChineseName { get; set; }
        public string? Labels { get; set; }
        public string? Description { get; set; }
        public string? ChineseDescription { get; set; }

        public List<FieldChange> CompareFields(TestCase other, IEnumerable<string> fields)
      => FieldComparer.CompareByNames(this, other, fields);
    }
}

namespace ThreatFramework.Core.CoreEntities
{
    public class Component : IFieldComparable<Component>
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public Guid LibraryGuid { get; set; }
        public Guid ComponentTypeGuid { get; set; }
        public string ComponentTypeName { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string Name { get; set; }
        public string? ImagePath { get; set; }
        public string? Labels { get; set; }
        public string? Version { get; set; }
        public string? Description { get; set; }
        public string? ChineseDescription { get; set; }

        public List<FieldChange> CompareFields(Component other, IEnumerable<string> fields)
       => FieldComparer.CompareByNames(this, other, fields);
    }
}

namespace ThreatFramework.Core.Models.CoreEntities
{
    public class Property
    {
        public int Id { get; set; }
        public int LibraryId { get; set; }
        public int PropertyTypeId { get; set; }
        public bool IsSelected { get; set; }
        public bool IsOptional { get; set; }
        public bool IsGlobal { get; set; }
        public bool IsHidden { get; set; }
        public bool IsOverridden { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public Guid Guid { get; set; }
        public string? Name { get; set; }
        public string? ChineseName { get; set; }
        public string? Labels { get; set; }
        public string? Description { get; set; }
        public string? ChineseDescription { get; set; }
    }
}

namespace ThreatFramework.Core.Models
{
    public class Component
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public int LibraryId { get; set; }
        public int ComponentTypeId { get; set; }
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
    }
}

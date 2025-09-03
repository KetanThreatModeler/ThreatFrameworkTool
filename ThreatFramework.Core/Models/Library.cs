namespace ThreatFramework.Core.Models
{
    public class Library
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public int DepartmentId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool Readonly { get; set; }
        public bool IsDefault { get; set; }
        public string Name { get; set; }
        public string? SharingType { get; set; }
        public string? Description { get; set; }
        public string? Labels { get; set; }
        public string? Version { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? ImageURL { get; set; }
    }
}

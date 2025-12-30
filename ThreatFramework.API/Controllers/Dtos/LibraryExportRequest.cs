namespace ThreatModeler.TF.API.Controllers.Dtos
{
    public class LibraryExportRequest
    {
        public List<Guid> LibraryIds { get; set; } = new List<Guid>();
    }
}

namespace ThreatModeler.TF.API.Controllers.Dtos
{
    public class LibraryExportRequest
    {
        public IEnumerable<Guid> LibraryIds { get; set; } = new List<Guid>();
    }
}

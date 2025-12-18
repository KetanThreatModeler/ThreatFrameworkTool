namespace ThreatModeler.TF.API.Controllers.Dtos
{
    public sealed class AssistRuleIndexWriteRequest
    {
        public List<Guid> LibraryGuids { get; set; } = new();
    }
}

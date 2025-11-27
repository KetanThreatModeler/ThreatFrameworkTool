namespace ThreatFramework.Infra.Contract.Index
{
    public interface IGuidIndexService
    {
        Task GenerateAsync(string? outputPath = null);
        Task RefreshAsync(string? path = null);
        int GetInt(Guid guid);
    }
}

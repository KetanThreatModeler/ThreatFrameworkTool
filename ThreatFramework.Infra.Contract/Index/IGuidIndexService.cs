namespace ThreatFramework.Infra.Contract.Index
{
    public interface IGuidIndexService
    {
        Task GenerateAsync(string? outputPath = null);
        Task GenerateForLibraryAsync(IEnumerable<Guid> libIds, string? outputPath = null);
        Task RefreshAsync(string? path = null);
        int GetInt(Guid guid);

        Guid GetGuid(int id);
        IReadOnlyCollection<int> GetIdsByLibraryAndType(Guid libraryId, EntityType entityType);

        // Convenience methods
        IReadOnlyCollection<int> GetComponentIds(Guid libraryId);
        IReadOnlyCollection<int> GetThreatIds(Guid libraryId);
        IReadOnlyCollection<int> GetSecurityRequirementIds(Guid libraryId);
        IReadOnlyCollection<int> GetPropertyIds(Guid libraryId);
        IReadOnlyCollection<int> GetTestCaseIds(Guid libraryId);
    }
}

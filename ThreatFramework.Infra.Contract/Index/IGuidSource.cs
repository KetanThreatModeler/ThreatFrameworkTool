namespace ThreatFramework.Infra.Contract.Index
{
    public interface IGuidSource
    {
        Task<IEnumerable<EntityIdentifier>> GetAllGuidsWithTypeAsync();

        Task<IEnumerable<EntityIdentifier>> GetGuidsWithTypeByLibraryIdsAsync(IEnumerable<Guid> libraryIds);
    }
}

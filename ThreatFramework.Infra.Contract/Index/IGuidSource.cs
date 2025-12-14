namespace ThreatFramework.Infra.Contract.Index
{
    public interface IGuidSource
    {
        Task<IEnumerable<EntityIdentifier>> GetAllGuidsWithTypeAsync();

        Task<IEnumerable<EntityIdentifier>> GetGuidsWithTypeByLibraryIdsAsync(IEnumerable<Guid> libraryIds);

        Task<IEnumerable<EntityIdentifier>> GetAllGuidsWithTypeAsyncForClient();

        Task<IEnumerable<EntityIdentifier>> GetGuidsWithTypeByLibraryIdsAsyncForClient(IEnumerable<Guid> libraryIds);
    }
}

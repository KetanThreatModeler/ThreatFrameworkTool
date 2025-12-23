namespace ThreatFramework.Infra.Contract.Index
{
    public interface ITRCGuidSource
    {
        Task<IEnumerable<EntityIdentifier>> GetAllGuidsWithTypeAsync();

        Task<IEnumerable<EntityIdentifier>> GetGuidsWithTypeByLibraryIdsAsync(IEnumerable<Guid> libraryIds);
    }
}

namespace ThreatFramework.Infra.Contract.Index
{
    public interface IGuidSource
    {
        Task<IEnumerable<EntityIdentifier>> GetAllGuidsWithTypeAsync();
    }
}

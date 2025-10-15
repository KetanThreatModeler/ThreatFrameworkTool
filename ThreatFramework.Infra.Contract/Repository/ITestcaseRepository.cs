using ThreatFramework.Core.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface ITestcaseRepository
    {
        Task<IEnumerable<TestCase>> GetTestcasesByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<TestCase>> GetReadOnlyTestcasesAsync();
        Task<IEnumerable<Guid>> GetGuidsAsync();
    }
}

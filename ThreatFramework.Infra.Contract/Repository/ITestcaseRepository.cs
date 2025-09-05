using ThreatFramework.Core.Models.CoreEntities;

namespace ThreatFramework.Infrastructure.Interfaces.Repositories
{
    public interface ITestcaseRepository
    {
        Task<IEnumerable<TestCase>> GetTestcasesByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<TestCase>> GetReadOnlyTestcasesAsync();
    }
}

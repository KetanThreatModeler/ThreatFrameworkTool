using ThreatModeler.TF.Core.Model.CoreEntities;

namespace ThreatFramework.Infra.Contract.Repository
{
    public interface ITestcaseRepository
    {
        Task<IEnumerable<TestCase>> GetTestcasesByLibraryIdAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<TestCase>> GetReadOnlyTestcasesAsync();
        Task<IEnumerable<Guid>> GetGuidsAsync();
        Task<IEnumerable<(Guid TestCaseGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync();

        Task<IEnumerable<(Guid TestCaseGuid, Guid LibraryGuid)>> GetGuidsAndLibraryGuidsAsync(IEnumerable<Guid> libraryIds);
        Task<IEnumerable<Guid>> GetGuidsByLibraryIds(IEnumerable<Guid> libraryIds);
    }
}

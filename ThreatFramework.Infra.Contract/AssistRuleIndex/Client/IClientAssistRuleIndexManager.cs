using ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Model;

namespace ThreatModeler.TF.Infra.Contract.AssistRuleIndex.Client
{
    public interface IClientAssistRuleIndexManager
    {
        Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAndWriteAsync(
          IEnumerable<Guid> libraryGuids);

        Task<IReadOnlyList<AssistRuleIndexEntry>> BuildAndWriteAsync();

        Task<IReadOnlyList<AssistRuleIndexEntry>> ReloadFromYamlAsync();
    }
}

using ThreatModeler.TF.Core.Model.AssistRules;

namespace ThreatModeler.TF.Infra.Contract.YamlRepository.AssistRules
{
    public interface IYamlResourceTypesValueReader
    {
        Task<ResourceTypeValues> GetResourceTypeValue(string path);
        Task<IEnumerable<ResourceTypeValues>> GetResourceTypeValues(IEnumerable<string> paths);
    }
}

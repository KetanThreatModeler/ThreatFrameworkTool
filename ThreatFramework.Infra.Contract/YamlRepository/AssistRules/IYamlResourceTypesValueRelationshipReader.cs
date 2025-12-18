using ThreatModeler.TF.Core.Model.AssistRules;

namespace ThreatModeler.TF.Infra.Contract.YamlRepository.AssistRules
{
    public interface IYamlResourceTypesValueRelationshipReader
    {
        Task<ResourceTypeValueRelationship> GetResourceTypeValueRelationship(string filePath);
        Task<IEnumerable<ResourceTypeValueRelationship>> GetResourceTypeValueRelationships(IEnumerable<string> filePaths);  

    }
}

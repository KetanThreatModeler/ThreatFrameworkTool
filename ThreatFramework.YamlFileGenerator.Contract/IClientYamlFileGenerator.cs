namespace ThreatFramework.YamlFileGenerator.Contract
{
    public interface IClientYamlFileGenerator
    {
        Task<(string path, int fileCount)> GenerateYamlFilesForSpecificLibraries(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForSpecificThreats(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForSpecificComponents(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForAllComponentTypes(string path); 
        Task<(string path, int fileCount)> GenerateYamlFilesForSpecificSecurityRequirements(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForSpecificTestCases(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForSpecificProperties(string path, List<Guid> propertyIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForPropertyTypes(string path);
        Task<(string path, int fileCount)> GenerateYamlFilesForPropertyOptions(string path);
        Task<(string path, int fileCount)> GenerateYamlFilesForComponentSecurityRequirementMappings(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForComponentThreatMappings(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForComponentThreatSecurityRequirementMappings(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForThreatSecurityRequirementMappings(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyMappings(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionMappings(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionThreatMappings(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForComponentPropertyOptionThreatSecurityRequirementMappings(string path, List<Guid> libraryIds);

        Task<(string path, int fileCount)> GenerateYamlFilesForRelationships(string path);
        Task<(string path, int fileCount)> GenerateYamlFilesForResourceTypeValues(string path, List<Guid> libraryIds);
        Task<(string path, int fileCount)> GenerateYamlFilesForResourceTypeValueRelationships(string path, List<Guid> libraryIds);
    }
}

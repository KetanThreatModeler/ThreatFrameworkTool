namespace ThreatFramework.YamlFileGenerator.Contract
{
    public interface IYamlFileGenerator
    {
        Task<string> GenerateYamlFilesForThreats(string path);
        Task<string> GenerateYamlFilesForComponents(string path);
        Task<string> GenerateYamlFilesForLibraries(string path);
        Task<string> GenerateYamlFilesForSecurityRequirements(string path);
        Task<string> GenerateYamlFilesForProperties(string path);
        Task<string> GenerateYamlFilesForPropertyOptions(string path);
        Task<string> GenerateYamlFilesForTestCases(string path);
    }
}

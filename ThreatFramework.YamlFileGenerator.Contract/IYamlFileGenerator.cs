namespace ThreatFramework.YamlFileGenerator.Contract
{
    public interface IYamlFileGenerator
    {
        Task<(string path, int fileCount)> GenerateYamlFilesForThreats(string path);
        Task<(string path, int fileCount)> GenerateYamlFilesForComponents(string path);
        Task<(string path, int fileCount)> GenerateYamlFilesForLibraries(string path);
        Task<(string path, int fileCount)> GenerateYamlFilesForSecurityRequirements(string path);
        Task<(string path, int fileCount)> GenerateYamlFilesForProperties(string path);
        Task<(string path, int fileCount)> GenerateYamlFilesForPropertyOptions(string path);
        Task<(string path, int fileCount)> GenerateYamlFilesForTestCases(string path);
    }
}

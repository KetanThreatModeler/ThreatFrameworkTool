namespace ThreatFramework.Infrastructure.Configuration
{
    public class ThreatModelingOptions
    {
        public const string SectionName = "ThreatModeling";
        
        public string IndexYamlPath { get; set; } = string.Empty;
    }
}
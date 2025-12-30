using Microsoft.Extensions.Options;
using ThreatModeler.TF.Core.Model.CoreEntities;

namespace ThreatModeler.TF.API.ServiceRegister
{
    public sealed class PathOptionsPostConfigure : IPostConfigureOptions<PathOptions>
    {
        public void PostConfigure(string? name, PathOptions options)
        {
            var baseDir = AppContext.BaseDirectory;

            options.IndexYaml = Normalize(baseDir, options.IndexYaml);
            options.TrcOutput = Normalize(baseDir, options.TrcOutput);
            options.ClientOutput = Normalize(baseDir, options.ClientOutput);
            options.AssistRuleIndexYaml = Normalize(baseDir, options.AssistRuleIndexYaml);
            options.ClientIndexYaml = Normalize(baseDir, options.ClientIndexYaml);
        }

        static string Normalize(string baseDir, string path) =>
            Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(baseDir, path));
    }
}

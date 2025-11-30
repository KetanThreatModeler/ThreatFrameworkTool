using ThreatFramework.Core.CoreEntities;
using ThreatModeler.TF.Core.Helper;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates
{
    internal static class ThreatTemplate
    {
        public static string Generate(Threat threat)
        {
            string yaml = new YamlBuilder()
                .AddChild("kind: threat")
                .AddChild("apiVersion: v1")
                .AddQuoted("guid", threat.Guid.ToString())
                .AddQuoted("name", threat.Name)
                .AddQuoted("libraryGuid", threat.LibraryGuid.ToString())
                .AddLabels("labels", threat.Labels.ToDelimitedString())
                .AddQuoted("description", threat.Description ?? string.Empty)
                .AddQuoted("reference", threat.Reference ?? string.Empty)
                .AddQuoted("intelligence", threat.Intelligence ?? string.Empty)
                .AddQuoted("chineseName", threat.ChineseName ?? string.Empty)
                .AddQuoted("chineseDescription", threat.ChineseDescription ?? string.Empty)
                .AddParent("flags:", fb =>
                {
                    _ = fb.AddBool("automated", threat.Automated);
                    _ = fb.AddBool("isHidden", threat.IsHidden);
                    _ = fb.AddBool("isOverridden", threat.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}

using ThreatModeler.TF.Core.Model.CoreEntities;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates
{
    public static class TestCaseTemplate
    {
        public static string Generate(TestCase testCase)
        {
            string yaml = new YamlBuilder()
                .AddChild("kind: test-case")
                .AddChild("apiVersion: v1")
                .AddQuoted("guid", testCase.Guid.ToString())
                .AddQuoted("name", testCase.Name)
                .AddQuoted("libraryGuid", testCase.LibraryId.ToString())
                .AddLabels("labels", testCase.Labels)
                .AddQuoted("description", testCase.Description ?? string.Empty)
                .AddQuoted("chineseName", testCase.ChineseName ?? string.Empty)
                .AddQuoted("chineseDescription", testCase.ChineseDescription ?? string.Empty)
                .AddParent("flags:", b2 =>
                {
                    _ = b2.AddBool("isHidden", testCase.IsHidden);
                    _ = b2.AddBool("isOverridden", testCase.IsOverridden);
                })
                .Build();

            return yaml;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Global;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.Global
{
    public static class ComponentTypeTemplate
    {
        public static string Generate(ComponentType componentType)
        {
            if (componentType == null)
                throw new ArgumentNullException(nameof(componentType));

            var yaml = new YamlBuilder()
                .AddChild("kind: component-type-template")
                .AddQuoted("guid", componentType.Guid.ToString())
                .AddQuoted("libraryGuid", componentType.LibraryGuid.ToString())
                .AddQuoted("name", componentType.Name ?? string.Empty)
                .AddQuoted("description", componentType.Description ?? string.Empty)
                .AddQuoted("chineseName", componentType.ChineseName ?? string.Empty)
                .AddQuoted("chineseDescription", componentType.ChineseDescription ?? string.Empty)
                .AddParent("flags:", b2 =>
                {
                    b2.AddBool("isHidden", componentType.IsHidden);
                    b2.AddBool("isSecurityControl", componentType.IsSecurityControl);
                })
                .Build();

            return yaml;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Core.Global;

namespace ThreatModeler.TF.YamlFileGenerator.Implementation.Templates.Global
{
    public static class RiskTemplate
    {
        public static string Generate(Risk risk)
        {
            if (risk == null)
                throw new ArgumentNullException(nameof(risk));

            var yaml = new YamlBuilder()
                .AddChild("kind: risk")
                .AddQuoted("name", risk.Name ?? string.Empty)
                .AddQuoted("color", risk.Color)
                .AddQuoted("suggestedName", risk.SuggestedName)
                .AddQuoted("chineseName", risk.ChineseName)
                .AddQuoted("score", risk.Score.ToString())
                .Build();

            return yaml;
        }
    }
}

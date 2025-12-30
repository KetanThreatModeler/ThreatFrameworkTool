using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Infra.Implmentation.AssistRuleIndex.Common
{
    internal static class ResourceTypeValueNormalizer
    {
        // Remove *all* whitespace (leading, trailing, and in-between)
        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return Regex.Replace(value, @"\s+", "");
        }
    }
}

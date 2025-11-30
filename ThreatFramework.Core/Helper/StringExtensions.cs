using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Core.Helper
{
    public static class StringExtensions
    {
        public static List<string> ToListWithTrim(this string value, char delimiter = ',')
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            return value
                .Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item)) // Ensures " , " doesn't result in an empty string in the list
                .ToList();
        }
    }
}

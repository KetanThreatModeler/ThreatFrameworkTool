using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Core.Helper
{
    public static class ListExtensions
    {
        /// <summary>
        /// Converts a list of strings into a single string separated by a delimiter.
        /// Default delimiter is ",".
        /// Returns string.Empty if the list is null or empty.
        /// </summary>
        public static string ToDelimitedString(this IEnumerable<string> values, string delimiter = ",")
        {
            if (values == null || !values.Any())
            {
                return string.Empty;
            }

            // string.Join automatically handles the separation logic
            return string.Join(delimiter, values);
        }
    }
}

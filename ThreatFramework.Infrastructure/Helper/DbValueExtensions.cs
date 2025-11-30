using System;
using System.Collections.Generic;
using ThreatModeler.TF.Core.Helper;

namespace ThreatModeler.TF.Infra.Implmentation.Helper
{
    public static class DbValueExtensions
    {
       
        public static string ToSafeString(this object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            return value.ToString()?.Trim() ?? string.Empty;
        }

        public static List<string> ToLabelList(this object value)
        {
            var safeString = value.ToSafeString();

            return safeString.ToListWithTrim();
        }
    }
}
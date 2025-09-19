using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core
{
    public class FieldChange
    {
        public string FieldName { get; }
        public object? ExistingValue { get; }
        public object? NewValue { get; }

        public FieldChange(string fieldName, object? existingValue, object? newValue)
        {
            FieldName = fieldName;
            ExistingValue = existingValue;
            NewValue = newValue;
        }
    }
}

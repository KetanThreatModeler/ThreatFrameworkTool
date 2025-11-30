using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Core.CustomException
{
    public class FieldComparisonNotImplementedException : Exception
    {
        public FieldComparisonNotImplementedException(string className, string fieldName)
            : base($"Field '{fieldName}' is requested for comparison but not implemented in class '{className}'.")
        {
        }
    }
}

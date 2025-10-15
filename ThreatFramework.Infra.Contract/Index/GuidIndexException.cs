using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract.Index
{
    public class GuidIndexException : Exception
    {
        public GuidIndexException(string message) : base(message) { }
        public GuidIndexException(string message, Exception inner) : base(message, inner) { }
    }
}

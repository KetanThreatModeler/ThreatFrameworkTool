using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Drift.Contract.Model
{
    public class DriftAnalyzeResponse
    {
        public List<LibraryDrift> Libraries { get; init; } = new();
        public GlobalDrift Global { get; set; }    }
}

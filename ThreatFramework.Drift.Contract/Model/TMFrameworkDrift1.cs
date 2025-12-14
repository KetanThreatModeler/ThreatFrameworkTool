using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Global;

namespace ThreatModeler.TF.Drift.Contract.Model
{
    public class TMFrameworkDrift1
    {
        public List<LibraryDrift1> ModifiedLibraries { get; init; } = new();
        public List<AddedLibrary1> AddedLibraries { get; init; } = new();
        public List<DeletedLibrary1> DeletedLibraries { get; init; } = new();
        public GlobalDrift1 Global { get; set; }
    }


    public class GlobalDrift1
    {
        public EntityDiff<PropertyOption> PropertyOptions { get; init; } = new();
        public EntityDiff<PropertyType> PropertyTypes { get; init; } = new();
        public EntityDiff<ComponentType> ComponentTypes { get; init; } = new();
    }
}

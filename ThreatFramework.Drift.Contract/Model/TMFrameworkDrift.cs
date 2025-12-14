using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Global;

namespace ThreatModeler.TF.Drift.Contract.Model
{
    public class TMFrameworkDrift
    {
        public List<LibraryDrift> ModifiedLibraries { get; init; } = new();
        public List<AddedLibrary> AddedLibraries { get; init; } = new();
        public List<DeletedLibrary> DeletedLibraries { get; init; } = new();
        public GlobalDrift Global { get; set; }
    }


    public class GlobalDrift
    {
        public EntityDiff<PropertyOption> PropertyOptions { get; init; } = new();
        public EntityDiff<PropertyType> PropertyTypes { get; init; } = new();
        public EntityDiff<ComponentType> ComponentTypes { get; init; } = new();
    }
}

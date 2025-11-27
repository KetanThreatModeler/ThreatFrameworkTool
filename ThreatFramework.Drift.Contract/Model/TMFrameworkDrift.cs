using ThreatFramework.Core.CoreEntities;
using ThreatModeler.TF.Core.Global;

namespace ThreatFramework.Drift.Contract.Model
{
    public class TMFrameworkDrift
    {
        public List<LibraryDrift> ModifiedLibraries { get; init; } = new();
        public List<AddedLibrary> AddedLibraries { get; init; } = new();
        public List<DeletedLibrary> DeletedLibraries { get; init; } = new();
        public GlobalDrift Global { get; set; }
    }

    public class AddedLibrary
    {
        public Library Library { get; init; }
        public List<AddedComponent> Components { get; init; } = new();
        public List<Threat> Threats { get; init; } = new();
        public List<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public List<TestCase> TestCases { get; init; } = new();
        public List<Property> Properties { get; init; } = new();
    }

    public class DeletedLibrary
    {
        public Guid LibraryGuid { get; init; }
        public string LibraryName { get; init; } = string.Empty;
        public List<DeletedComponent> Components { get; init; } = new();
        public List<Threat> Threats { get; init; } = new();
        public List<SecurityRequirement> SecurityRequirements { get; init; } = new();
        public List<TestCase> TestCases { get; init; } = new();
        public List<Property> Properties { get; init; } = new();

    }

    public class GlobalDrift
    {
        public EntityDiff<PropertyOption> PropertyOptions { get; init; } = new();
        public EntityDiff<PropertyType> PropertyTypes { get; init; } = new();
    }

}
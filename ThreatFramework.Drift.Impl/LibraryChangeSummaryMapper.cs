using ThreatFramework.Core;
using ThreatModeler.TF.Drift.Contract;
using ThreatModeler.TF.Drift.Contract.Dto;
using ThreatModeler.TF.Drift.Contract.Model;

public sealed class LibraryChangeSummaryMapper : ILibraryChangeSummaryMapper
{
    public IReadOnlyList<LibraryChangeSummaryDto> Map(TMFrameworkDrift drift)
    {
        if (drift is null) return Array.Empty<LibraryChangeSummaryDto>();

        var result = new List<LibraryChangeSummaryDto>();

        // Added
        foreach (var a in drift.AddedLibraries)
        {
            var lib = a?.Library;
            if (lib is null) continue;

            result.Add(new LibraryChangeSummaryDto
            {
                Guid = lib.Guid,
                LibraryName = lib.Name ?? string.Empty,
                Operation = "Added",
                LocalLibraryVersion = lib.Version,
                LibraryVersion = lib.Version,
                ReleaseNote = lib.ReleaseNotes
            });
        }

        // Removed
        foreach (var d in drift.DeletedLibraries)
        {
            if (d is null) continue;

            result.Add(new LibraryChangeSummaryDto
            {
                Guid = d.Library.Guid,
                LibraryName = d.Library.Name ?? string.Empty,
                Operation = "Removed",
                LocalLibraryVersion = d.Library.Version,
                LibraryVersion = d.Library.Version,
                ReleaseNote = d.Library.ReleaseNotes
            });
        }

        // Modified
        foreach (var m in drift.ModifiedLibraries)
        {
            var lib = m?.library;
            if (lib is null) continue;

            // These two lines are the whole point:
            var oldVersion = GetOldValue(m.LibraryChanges, "Version");
            var newVersion = GetNewValue(m.LibraryChanges, "Version") ?? lib.Version;

            var releaseNotes = GetNewValue(m.LibraryChanges, "ReleaseNotes") ?? lib.ReleaseNotes;

            result.Add(new LibraryChangeSummaryDto
            {
                Guid = lib.Guid,
                LibraryName = lib.Name ?? string.Empty,
                Operation = "Modified",
                LocalLibraryVersion = oldVersion,
                LibraryVersion = newVersion,
                ReleaseNote = releaseNotes
            });
        }

        return result;
    }

    private static string? GetOldValue(IEnumerable<FieldChange>? changes, string fieldName)
    {
        if (changes is null) return null;

        foreach (var c in changes)
        {
            if (c is null) continue;
            if (c.FieldName != fieldName) continue;

            return string.IsNullOrWhiteSpace(c.ExistingValue.ToString()) ? null : c.ExistingValue.ToString().Trim();
        }

        return null;
    }

    private static string? GetNewValue(IEnumerable<FieldChange>? changes, string fieldName)
    {
        if (changes is null) return null;

        foreach (var c in changes)
        {
            if (c is null) continue;
            if (c.FieldName != fieldName) continue;

            return string.IsNullOrWhiteSpace(c.NewValue.ToString()) ? null : c.NewValue.ToString().Trim();
        }

        return null;
    }
}

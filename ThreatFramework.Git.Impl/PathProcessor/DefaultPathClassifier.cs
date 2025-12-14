using ThreatModeler.TF.Git.Contract.Common;
using ThreatModeler.TF.Git.Contract.Models;
using ThreatModeler.TF.Git.Contract.PathProcessor;

namespace ThreatModeler.TF.Git.Implementation.PathProcessor
{
    public sealed class DefaultPathClassifier : IPathClassifier
    {
        public DomainPathInfo Classify(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(relativePath));
            }

            var normalized = relativePath.Replace('\\', '/');
            var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                return new DomainPathInfo(DomainEntityType.Unknown, null);
            }

            // ───────────── MAPPINGS ─────────────
            if (parts[0].Equals("mappings", StringComparison.OrdinalIgnoreCase) && parts.Length > 1)
            {
                return new DomainPathInfo(
                    MapMappingFolderToEntity(parts[1]),
                    libraryId: null);
            }

            // ───────────── GLOBAL ─────────────
            if (parts[0].Equals("global", StringComparison.OrdinalIgnoreCase) && parts.Length > 1)
            {
                return new DomainPathInfo(
                    MapGlobalFolderToEntity(parts[1]),
                    libraryId: null);
            }

            // ───────────── LIBRARIES (01, 06, 50, 34, ...) ─────────────
            var libraryId = parts[0];

            // Library definition file: 01/01.yaml, 06/06.yaml, etc.
            if (parts.Length == 2 && IsLibraryDefinitionFile(libraryId, parts[1]))
            {
                return new DomainPathInfo(DomainEntityType.Library, libraryId);
            }

            // Library subfolders like components/, threats/, etc.
            if (parts.Length > 1)
            {
                var folderName = parts[1].ToLowerInvariant();

                return new DomainPathInfo(
                    MapLibraryFolderToEntity(folderName),
                    libraryId);
            }

            return new DomainPathInfo(DomainEntityType.Unknown, null);
        }

        private static bool IsLibraryDefinitionFile(string libraryId, string fileName)
        {
            if (!fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            return string.Equals(nameWithoutExtension, libraryId, StringComparison.OrdinalIgnoreCase);
        }

        private static DomainEntityType MapLibraryFolderToEntity(string folderName) =>
            folderName switch
            {
                FolderNames.Components => DomainEntityType.Components,
                FolderNames.SecurityRequirements => DomainEntityType.SecurityRequirements,
                FolderNames.TestCases => DomainEntityType.TestCases,
                FolderNames.Threats => DomainEntityType.Threats,
                FolderNames.Properties => DomainEntityType.Properties,
                _ => DomainEntityType.Unknown
            };

        private static DomainEntityType MapGlobalFolderToEntity(string folderName) =>
            folderName.ToLowerInvariant() switch
            {
                FolderNames.ComponentType => DomainEntityType.ComponentType,
                FolderNames.PropertyType => DomainEntityType.PropertyType,
                FolderNames.PropertyOptions => DomainEntityType.PropertyOptions,
                _ => DomainEntityType.Unknown
            };

        private static DomainEntityType MapMappingFolderToEntity(string folderName) =>
            folderName.ToLowerInvariant() switch
            {
                FolderNames.ComponentProperty => DomainEntityType.ComponentProperty,
                FolderNames.ComponentPropertyOption => DomainEntityType.ComponentPropertyOptions,
                FolderNames.ComponentPropertyOptionThreat => DomainEntityType.ComponentPropertyOptionThreats,
                FolderNames.ComponentPropertyOptionThreatSecurityRequirement
                                                                    => DomainEntityType.ComponentPropertyOptionThreatSecurityRequirements,
                FolderNames.ComponentThreat => DomainEntityType.ComponentThreat,
                FolderNames.ComponentThreatSecurityRequirement => DomainEntityType.ComponentThreatSecurityRequirements,
                FolderNames.ComponentSecurityRequirement => DomainEntityType.ComponentSecurityRequirements,
                FolderNames.ThreatSecurityRequirement => DomainEntityType.ThreatSecurityRequirements,
                _ => DomainEntityType.Unknown
            };
    }
}

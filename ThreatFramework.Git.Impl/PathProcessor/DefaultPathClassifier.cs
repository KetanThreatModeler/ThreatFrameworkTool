using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                "components" => DomainEntityType.Components,
                "security-requirements" => DomainEntityType.SecurityRequirements,
                "testcases" => DomainEntityType.TestCases,
                "threats" => DomainEntityType.Threats,
                "properties" => DomainEntityType.Properties,
                _ => DomainEntityType.Unknown
            };

        private static DomainEntityType MapGlobalFolderToEntity(string folderName) =>
            folderName.ToLowerInvariant() switch
            {
                "component-type" => DomainEntityType.ComponentType,
                "property-type" => DomainEntityType.PropertyType,
                "property-options" => DomainEntityType.PropertyOptions,
                _ => DomainEntityType.Unknown
            };

        private static DomainEntityType MapMappingFolderToEntity(string folderName) =>
            folderName.ToLowerInvariant() switch
            {
                "component-property" => DomainEntityType.ComponentProperty,
                "component-property-options" => DomainEntityType.ComponentPropertyOptions,
                "component-property-option-threats" => DomainEntityType.ComponentPropertyOptionThreats,
                "component-property-option-threat-security-requirements"
                                                                    => DomainEntityType.ComponentPropertyOptionThreatSecurityRequirements,
                "component-threat" => DomainEntityType.ComponentThreat,
                "component-threat-security-requirements" => DomainEntityType.ComponentThreatSecurityRequirements,
                "component-security-requirements" => DomainEntityType.ComponentSecurityRequirements,
                "threat-security-requirements" => DomainEntityType.ThreatSecurityRequirements,
                _ => DomainEntityType.Unknown
            };
    }
}
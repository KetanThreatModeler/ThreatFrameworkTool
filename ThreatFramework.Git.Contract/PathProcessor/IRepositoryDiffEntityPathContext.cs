using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Git.Contract.PathProcessor
{
    public interface IRepositoryDiffEntityPathContext
    {
        // ───────── Library definition (01/01.yaml, 06/06.yaml, ...) ─────────
        EntityFileChangeSet GetLibraryFileChanges();
        IReadOnlyCollection<LibraryEntityFileChanges> GetLibraryFileChangesByLibrary();

        // ───────── Library-specific entities – aggregated (ignore library id) ─────────
        EntityFileChangeSet GetComponentFileChanges();
        EntityFileChangeSet GetSecurityRequirementFileChanges();
        EntityFileChangeSet GetTestCaseFileChanges();
        EntityFileChangeSet GetThreatFileChanges();
        EntityFileChangeSet GetPropertyFileChanges();

        // ───────── Library-specific entities – library-wise ─────────
        IReadOnlyCollection<LibraryEntityFileChanges> GetComponentFileChangesByLibrary();
        IReadOnlyCollection<LibraryEntityFileChanges> GetSecurityRequirementFileChangesByLibrary();
        IReadOnlyCollection<LibraryEntityFileChanges> GetTestCaseFileChangesByLibrary();
        IReadOnlyCollection<LibraryEntityFileChanges> GetThreatFileChangesByLibrary();
        IReadOnlyCollection<LibraryEntityFileChanges> GetPropertyFileChangesByLibrary();

        // ───────── Global entities ─────────
        EntityFileChangeSet GetComponentTypeFileChanges();
        EntityFileChangeSet GetPropertyTypeFileChanges();
        EntityFileChangeSet GetPropertyOptionsFileChanges();

        // ───────── Mapping entities ─────────
        EntityFileChangeSet GetComponentPropertyMappingFileChanges();
        EntityFileChangeSet GetComponentPropertyOptionsMappingFileChanges();
        EntityFileChangeSet GetComponentPropertyOptionThreatsMappingFileChanges();
        EntityFileChangeSet GetComponentPropertyOptionThreatSecurityRequirementsMappingFileChanges();
        EntityFileChangeSet GetComponentThreatMappingFileChanges();
        EntityFileChangeSet GetComponentThreatSecurityRequirementsMappingFileChanges();
        EntityFileChangeSet GetComponentSecurityRequirementsMappingFileChanges();
        EntityFileChangeSet GetThreatSecurityRequirementsMappingFileChanges();
    }
}

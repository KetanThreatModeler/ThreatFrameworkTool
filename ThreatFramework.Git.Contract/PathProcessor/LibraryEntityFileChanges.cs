using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Git.Contract.PathProcessor
{
    public sealed class LibraryEntityFileChanges
    {
        public LibraryEntityFileChanges(string libraryId, EntityFileChangeSet fileChanges)
        {
            LibraryId = libraryId ?? throw new ArgumentNullException(nameof(libraryId));
            FileChanges = fileChanges ?? throw new ArgumentNullException(nameof(fileChanges));
        }

        public string LibraryId { get; }
        public EntityFileChangeSet FileChanges { get; }
    }
}

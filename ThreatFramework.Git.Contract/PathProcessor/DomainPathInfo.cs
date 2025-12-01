using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Git.Contract.Models;

namespace ThreatModeler.TF.Git.Contract.PathProcessor
{
    public sealed class DomainPathInfo
    {
        public DomainPathInfo(DomainEntityType entityType, string? libraryId)
        {
            EntityType = entityType;
            LibraryId = libraryId;
        }

        /// <summary>
        /// High-level entity type (components, threats, mapping/component-threat, etc.).
        /// </summary>
        public DomainEntityType EntityType { get; }

        /// <summary>
        /// Library id (e.g. "01", "06", "50", "34") when applicable, otherwise null for global/mappings.
        /// </summary>
        public string? LibraryId { get; }
    }
}

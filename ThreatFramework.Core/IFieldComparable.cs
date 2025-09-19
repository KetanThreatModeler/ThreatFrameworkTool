using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core
{
    public interface IFieldComparable<T>
    {
        /// <summary>
        /// Compare this entity with <paramref name="other"/> using the provided list of field names.
        /// Must throw if any requested field name is not found on the entity.
        /// Field names should be matched case-insensitively.
        /// </summary>
        IReadOnlyList<FieldChange> CompareFields(T other, IEnumerable<string> fields);
    }

}

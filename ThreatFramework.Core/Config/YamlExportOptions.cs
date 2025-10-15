using System.ComponentModel.DataAnnotations;

namespace ThreatFramework.Core.Config
{
    public sealed class YamlExportOptions
    {
        [Required] public TenantExportOptions Trc { get; init; } = default!;
        [Required] public TenantExportOptions Client { get; init; } = default!;
    }

    public sealed class TenantExportOptions
    {
        [Required, MinLength(1)]
        public List<Guid> LibraryIds { get; init; } = new();

        [Required, MinLength(1)]
        public string OutputPath { get; init; } = string.Empty;
    }
}

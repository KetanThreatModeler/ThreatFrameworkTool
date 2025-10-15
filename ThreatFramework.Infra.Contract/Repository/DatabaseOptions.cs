using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Infra.Contract.Repository
{
    public sealed class DatabaseOptions
    {
        [Required] public string TrcConnectionString { get; set; } = default!;
        [Required] public string ClientConnectionString { get; set; } = default!;
    }
}

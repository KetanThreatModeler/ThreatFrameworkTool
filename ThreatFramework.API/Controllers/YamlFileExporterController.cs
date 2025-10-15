using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ThreatFramework.Core.Config;
using ThreatFramework.YamlFileGenerator.Contract;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class YamlExportsController : ControllerBase
    {
        private readonly ILogger<YamlExportsController> _logger;
        private readonly IYamlFileGeneratorForClient _clientGenerator;
        private readonly IYamlFilesGeneratorForTRC _trcGenerator;
        private readonly YamlExportOptions _exportOptions;

        public YamlExportsController(
            ILogger<YamlExportsController> logger,
            IYamlFileGeneratorForClient clientGenerator,
            IYamlFilesGeneratorForTRC trcGenerator,
            IOptions<YamlExportOptions> exportOptions)
        {
            _logger = logger;
            _clientGenerator = clientGenerator;
            _trcGenerator = trcGenerator;
            _exportOptions = exportOptions.Value;
        }

        /// <summary>
        /// Generate YAML files for the Client tenant.
        /// Uses OutputPath from appsettings.json YamlExport:Client:OutputPath.
        /// </summary>
        [HttpPost("client")]
        public async Task<IActionResult> GenerateClientAsync(CancellationToken ct)
        {
            var path = _exportOptions.Client.OutputPath;
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Client output path not configured in appsettings.json");

            _logger.LogInformation("Starting Client YAML export to {Output}", path);
            await _clientGenerator.GenerateAsync(path);
            _logger.LogInformation("Completed Client YAML export to {Output}", path);

            return Ok(new { tenant = "Client", outputPath = path, status = "completed" });
        }

        /// <summary>
        /// Generate YAML files for the TRC tenant.
        /// Uses OutputPath from appsettings.json YamlExport:Trc:OutputPath.
        /// </summary>
        [HttpPost("trc")]
        public async Task<IActionResult> GenerateTrcAsync(CancellationToken ct)
        {
            var path = _exportOptions.Trc.OutputPath;
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("TRC output path not configured in appsettings.json");

            _logger.LogInformation("Starting TRC YAML export to {Output}", path);
            await _trcGenerator.GenerateAsync(path);
            _logger.LogInformation("Completed TRC YAML export to {Output}", path);

            return Ok(new { tenant = "TRC", outputPath = path, status = "completed" });
        }
    }
}

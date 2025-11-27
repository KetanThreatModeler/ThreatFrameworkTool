using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatModeler.TF.Core.Config;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class YamlExportsController : ControllerBase
    {
        private readonly ILogger<YamlExportsController> _logger;
        private readonly IYamlFileGeneratorForClient _clientGenerator;
        private readonly IYamlFilesGeneratorForTRC _trcGenerator;
        private readonly PathOptions _exportOptions;

        public YamlExportsController(
            ILogger<YamlExportsController> logger,
            IYamlFileGeneratorForClient clientGenerator,
            IYamlFilesGeneratorForTRC trcGenerator,
            IOptions<PathOptions> exportOptions)
        {
            _logger = logger;
            _clientGenerator = clientGenerator;
            _trcGenerator = trcGenerator;
            _exportOptions = exportOptions.Value;
        }

       
        [HttpPost("client")]
        public async Task<IActionResult> GenerateClientAsync(CancellationToken ct)
        {
            var path = _exportOptions.ClientOutput;
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Client output path not configured in appsettings.json");

            _logger.LogInformation("Starting Client YAML export to {Output}", path);
            await _clientGenerator.GenerateForLibraryIdsAsync(path, new List<Guid> { });
            _logger.LogInformation("Completed Client YAML export to {Output}", path);

            return Ok(new { tenant = "Client", outputPath = path, status = "completed" });
        }

      
        [HttpPost("trc/readonly")]
        public async Task<IActionResult> GenerateTrcAsync(CancellationToken ct)
        {
            var path = _exportOptions.TrcOutput;
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("TRC output path not configured in appsettings.json");

            _logger.LogInformation("Starting TRC YAML export to {Output}", path);
            await _trcGenerator.GenerateForReadOnlyLibraryAsync(path);
            _logger.LogInformation("Completed TRC YAML export to {Output}", path);

            return Ok(new { tenant = "TRC", outputPath = path, status = "completed" });
        }

        [HttpPost("trc")]
        public async Task<IActionResult> GenerateTrcAsync(List<Guid> libraryIds, CancellationToken ct)
        {
            var path = _exportOptions.TrcOutput;
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("TRC output path not configured in appsettings.json");

            _logger.LogInformation("Starting TRC YAML export to {Output}", path);
            await _trcGenerator.GenerateForLibraryIdsAsync(path, libraryIds);
            _logger.LogInformation("Completed TRC YAML export to {Output}", path);

            return Ok(new { tenant = "TRC", outputPath = path, status = "completed" });
        }
    }
}

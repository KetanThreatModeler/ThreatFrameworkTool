using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ThreatFramework.Core.Config;
using ThreatFramework.Drift.Contract.FolderDiff;
using ThreatFramework.YamlFileGenerator.Contract.Model;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiffNewFolderToFolderController : ControllerBase
    {
        private readonly IFolderDiffService _service;
        private readonly IConfiguration _configuration;
        private readonly YamlExportOptions _exportOptions;

        public DiffNewFolderToFolderController(IFolderDiffService service, IConfiguration configuration,
            IOptions<YamlExportOptions> exportOptions)
        {
            _service = service;
            _configuration = configuration;
            _exportOptions = exportOptions.Value;
        }

        [HttpPost]
        public ActionResult<FolderComparisionResult> Post()
        {
            var goldenPath = _exportOptions.Trc.OutputPath;
            var clientPath= _exportOptions.Client.OutputPath;

            if (string.IsNullOrWhiteSpace(goldenPath))
                return BadRequest("TRC output path not configured in appsettings.json");
            
            if (string.IsNullOrWhiteSpace(clientPath))
                return BadRequest("Client output path not configured in appsettings.json");

            var result = _service.Compare(goldenPath, clientPath);
            return Ok(result);
        }
    }
}

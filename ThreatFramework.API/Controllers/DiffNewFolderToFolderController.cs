using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ThreatFramework.Drift.Contract.FolderDiff;
using ThreatModeler.TF.Core.CoreEntities;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiffNewFolderToFolderController : ControllerBase
    {
        private readonly IFolderDiffService _service;
        private readonly IConfiguration _configuration;
        private readonly PathOptions _exportOptions;

        public DiffNewFolderToFolderController(IFolderDiffService service, IConfiguration configuration,
            IOptions<PathOptions> exportOptions)
        {
            _service = service;
            _configuration = configuration;
            _exportOptions = exportOptions.Value;
        }

        [HttpPost]
        public ActionResult<FolderComparisionResult> Post()
        {
            var goldenPath = _exportOptions.TrcOutput;
            var clientPath= _exportOptions.ClientOutput;

            if (string.IsNullOrWhiteSpace(goldenPath))
                return BadRequest("TRC output path not configured in appsettings.json");
            
            if (string.IsNullOrWhiteSpace(clientPath))
                return BadRequest("Client output path not configured in appsettings.json");

            var result = _service.Compare(goldenPath, clientPath);
            return Ok(result);
        }
    }
}

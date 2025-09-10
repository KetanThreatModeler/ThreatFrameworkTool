using Microsoft.AspNetCore.Mvc;
using ThreatFramework.Core.Git;
using ThreatFramework.Git.Contract;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiffSummaryController : ControllerBase
    {
        private readonly IDiffSummaryService _service;
        private readonly ILogger<DiffSummaryController> _log;

        public DiffSummaryController(IDiffSummaryService service, ILogger<DiffSummaryController> log)
        {
            _service = service;
            _log = log;
        }

        [HttpPost]
        public async Task<ActionResult<DiffSummaryResponse>> Post([FromBody] DiffSummaryRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var result = await _service.CompareAsync(request, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error running diff");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

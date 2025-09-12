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

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];

            _log.LogInformation("Starting diff comparison {RequestId} for RemoteRepo: {RemoteRepo}, TargetPath: {TargetPath}",
                requestId, request.RemoteRepoUrl, request.TargetPath);

            try
            {
                var result = await _service.CompareAsync(request, ct);

                stopwatch.Stop();
                _log.LogInformation("Diff comparison {RequestId} completed successfully in {ElapsedMs}ms",
                    requestId, stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _log.LogError(ex, "Error running diff comparison {RequestId} after {ElapsedMs}ms. RemoteRepo: {RemoteRepo}, TargetPath: {TargetPath}",
                    requestId, stopwatch.ElapsedMilliseconds, request.RemoteRepoUrl, request.TargetPath);

                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

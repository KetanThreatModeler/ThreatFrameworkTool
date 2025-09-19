using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ThreatFramework.Core.Git;
using ThreatFramework.Git.Contract;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FolderToFolderDiffController : ControllerBase
    {
        private readonly IFolderToFolderDiffService _diffService;
        private readonly ILogger<FolderToFolderDiffController> _logger;

        public FolderToFolderDiffController(
            IFolderToFolderDiffService diffService,
            ILogger<FolderToFolderDiffController> logger)
        {
            _diffService = diffService;
            _logger = logger;
        }

        [HttpPost("compare")]
        public async Task<ActionResult<DiffSummaryResponse>> CompareAsync(
            [FromBody] FolderToFolderDiffRequest request,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("Starting folder comparison. BaselinePath: {BaselinePath}, TargetPath: {TargetPath}", 
                    request?.BaselineFolderPath, request?.TargetFolderPath);

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _diffService.CompareAsync(request, cancellationToken);
                
                stopwatch.Stop();
                _logger.LogInformation("Folder comparison completed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return Ok(result);
            }
            catch (DirectoryNotFoundException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Directory not found during folder comparison after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Invalid arguments provided for folder comparison after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error during folder comparison after {ElapsedMs}ms. Exception Type: {ExceptionType}, Message: {Message}", 
                    stopwatch.ElapsedMilliseconds, ex.GetType().Name, ex.Message);
                return StatusCode(500, "An unexpected error occurred while comparing folders");
            }
        }
    }
}

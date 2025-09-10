using Microsoft.AspNetCore.Mvc;
using ThreatFramework.Core.Git;
using ThreatFramework.Git.Contract;

namespace ThreatFramework.API
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
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _diffService.CompareAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogWarning(ex, "Directory not found during folder comparison");
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid arguments provided for folder comparison");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during folder comparison");
                return StatusCode(500, "An unexpected error occurred while comparing folders");
            }
        }
    }
}

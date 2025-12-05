using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using ThreatModeler.TF.Core.CoreEntities;
using ThreatModeler.TF.Git.Contract;

namespace ThreatModeler.TF.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GitDiffController : ControllerBase
    {
        private readonly IGitFolderDiffService _diffService;
        private readonly PathOptions _pathOption;
        private readonly ILogger<GitDiffController> _logger;

        public GitDiffController(
            IGitFolderDiffService diffService,
            IOptions<PathOptions> exportOptions,
            ILogger<GitDiffController> logger)
        {
            _diffService = diffService ?? throw new ArgumentNullException(nameof(diffService));
            _pathOption = exportOptions?.Value ?? throw new ArgumentNullException(nameof(exportOptions));
            _logger = logger;
        }

        /// <summary>
        /// Compares specific folders between the configured TRC (Repo1) and Client (Repo2) paths.
        /// </summary>
        [HttpPost("folders")]
        public async Task<IActionResult> CompareFolders([FromBody] CompareFoldersRequest request)
        {
            if (request.Folders == null || !request.Folders.Any())
                return BadRequest("Folder list cannot be empty.");

            try
            {
                ValidatePaths();

                _logger.LogInformation("Initiating Folder Comparison.");

                var report = await _diffService.CompareFoldersAsync(
                    _pathOption.TrcOutput,    // Repo 1
                    _pathOption.ClientOutput, // Repo 2
                    request.Folders);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compare folders.");
                return StatusCode(500, new { Message = "Internal error during comparison", Error = ex.Message });
            }
        }

        [HttpPost("prefix")]
        public async Task<IActionResult> CompareByPrefix([FromBody] ComparePrefixRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FolderPath))
                return BadRequest("FolderPath is required.");

            if (request.Prefixes == null || !request.Prefixes.Any())
                return BadRequest("Prefix list cannot be empty.");

            try
            {
                ValidatePaths();

                _logger.LogInformation("Initiating Prefix Comparison for folder: {Folder}", request.FolderPath);

                var report = await _diffService.CompareByPrefixAsync(
                    _pathOption.TrcOutput,
                    _pathOption.ClientOutput,
                    request.FolderPath,
                    request.Prefixes);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compare by prefix.");
                return StatusCode(500, new { Message = "Internal error during prefix comparison", Error = ex.Message });
            }
        }

        /// <summary>
        /// Compares the full repositories while ignoring specific files (e.g. index.yaml).
        /// </summary>
        [HttpPost("full-repo")]
        public async Task<IActionResult> CompareFullRepo([FromBody] CompareFullRepoRequest request)
        {
            try
            {
                ValidatePaths();

                _logger.LogInformation("Initiating Full Repo Comparison.");

                var report = await _diffService.CompareRepoWithExclusionsAsync(
                    _pathOption.TrcOutput,
                    _pathOption.ClientOutput,
                    request.FilesToIgnore);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compare full repository.");
                return StatusCode(500, new { Message = "Internal error during full comparison", Error = ex.Message });
            }
        }

        private void ValidatePaths()
        {
            if (string.IsNullOrWhiteSpace(_pathOption.TrcOutput) || string.IsNullOrWhiteSpace(_pathOption.ClientOutput))
            {
                throw new InvalidOperationException("Repository paths are not configured in AppSettings.");
            }
        }
    }

    public class CompareFoldersRequest
    {
        [Required]
        public List<string> Folders { get; set; } = new();
    }

    public class ComparePrefixRequest
    {
        [Required]
        public string FolderPath { get; set; }
        [Required]
        public List<string> Prefixes { get; set; } = new();
    }

    public class CompareFullRepoRequest
    {
        public List<string> FilesToIgnore { get; set; } = new();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ThreatModeler.TF.Git.Contract;
using ThreatModeler.TF.Git.Contract.Models;

namespace ThreatModeler.TF.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GitController : ControllerBase
    {
        private readonly IGitService _gitService;
        private readonly GitSettings _defaultSettings;
        private readonly ILogger<GitController> _logger;

        public GitController(
            IGitService gitService,
            IOptions<GitSettings> options,
            ILogger<GitController> logger)
        {
            _gitService = gitService;
            _defaultSettings = options.Value;
            _logger = logger;
        }

        [HttpPost("sync")]
        public IActionResult Sync([FromBody] GitSettings request)
        {
            try
            {
                _logger.LogInformation("Received Sync Request.");

                // Create Base Settings
                var config = MergeBaseSettings(request);

                _gitService.SyncRepository(config);

                return Ok(new { Message = "Synced successfully", Path = config.LocalPath });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("push")]
        public IActionResult Push([FromBody] GitCommitContext request)
        {
            try
            {
                _logger.LogInformation("Received Push Request.");

                // Create Derived Context (Config + Message)
                var context = PrepareCommitContext(request);

                _gitService.CommitAndPush(context);

                return Ok(new { Message = "Committed and Pushed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // --- Strategy 1: Merging Base Settings ---
        private GitSettings MergeBaseSettings(GitSettings request)
        {
            return new GitSettings
            {
                RepoUrl = request.RepoUrl ?? _defaultSettings.RepoUrl,
                LocalPath = request.LocalPath ?? _defaultSettings.LocalPath,
                Branch = request.Branch ?? _defaultSettings.Branch,
                Username = request.Username ?? _defaultSettings.Username,
                Password = request.Password ?? _defaultSettings.Password,
                AuthorName = request.AuthorName ?? _defaultSettings.AuthorName,
                AuthorEmail = request.AuthorEmail ?? _defaultSettings.AuthorEmail,
            };
        }

        // --- Strategy 2: Preparing Commit Context (Inheritance) ---
        private GitCommitContext PrepareCommitContext(GitCommitContext request)
        {
            // 1. Get the base config merged
            var baseConfig = MergeBaseSettings(request);

            // 2. Return the Derived class with the specific CommitMessage
            return new GitCommitContext
            {
                // Map Base Properties
                RepoUrl = baseConfig.RepoUrl,
                LocalPath = baseConfig.LocalPath,
                Branch = baseConfig.Branch,
                Username = baseConfig.Username,
                Password = baseConfig.Password,
                AuthorName = baseConfig.AuthorName,
                AuthorEmail = baseConfig.AuthorEmail,

                // Map Specific Property
                CommitMessage = request.CommitMessage
            };
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ThreatFramework.Core.Git;
using ThreatFramework.Drift.Contract;
using ThreatFramework.Drift.Contract.Model;
using ThreatFramework.Git.Contract;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DriftController : ControllerBase
    {
        private readonly IDriftService _driftService;
        private readonly IFolderToFolderDiffService _folderDiffService;
        
        public DriftController(IDriftService driftService, IFolderToFolderDiffService folderDiffService)
        {
            _driftService = driftService;
            _folderDiffService = folderDiffService;
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<DriftAnalyzeResponse>> Analyze([FromBody, Required] DriftAnalyzeRequest request, CancellationToken ct)
        {
            if (request is null) return BadRequest("Request body required.");
            var result = await _driftService.AnalyzeAsync(request, ct);
            return Ok(result);
        }

        private static DriftSummaryResponse? MapToDriftSummaryResponse(DiffSummaryResponse diffSummaryResponse)
        {
            if (diffSummaryResponse == null) return null;
            
            return new DriftSummaryResponse(
                diffSummaryResponse.RemoteRepoUrl,
                diffSummaryResponse.TargetPath,
                diffSummaryResponse.AddedCount,
                diffSummaryResponse.RemovedCount,
                diffSummaryResponse.ModifiedCount,
                diffSummaryResponse.RenamedCount,
                diffSummaryResponse.AddedFiles,
                diffSummaryResponse.RemovedFiles,
                diffSummaryResponse.ModifiedFiles
            );
        }

        private static DriftAnalyzeRequest MapToDriftAnalyzeRequest(string baselinePath, string targetPath, DiffSummaryResponse folderDiffResponse)
        {
            return new DriftAnalyzeRequest
            {
                BaselineFolderPath = baselinePath,
                TargetFolderPath = targetPath,
                DriftSummaryResponse = MapToDriftSummaryResponse(folderDiffResponse)

            };
        }

        [HttpPost("analyze-folders")]
        public async Task<ActionResult<DriftAnalyzeResponse>> AnalyzeFolders([FromQuery, Required] string baselinePath, [FromQuery, Required] string targetPath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(baselinePath)) return BadRequest("Baseline path is required.");
            if (string.IsNullOrWhiteSpace(targetPath)) return BadRequest("Target path is required.");

            var folderToFolderCompare = new FolderToFolderDiffRequest
            {
                BaselineFolderPath = targetPath,
                TargetFolderPath = baselinePath
            };
            
            // First get folder diff response
            var folderDiffResponse = await _folderDiffService.CompareAsync(folderToFolderCompare, ct);

            // Map folder diff response to DriftAnalyzeRequest
            var driftRequest = MapToDriftAnalyzeRequest(baselinePath, targetPath, folderDiffResponse);

            // Use IDriftService for analysis
            var result = await _driftService.AnalyzeAsync(driftRequest, ct);
            return Ok(result);
        }
    }
}

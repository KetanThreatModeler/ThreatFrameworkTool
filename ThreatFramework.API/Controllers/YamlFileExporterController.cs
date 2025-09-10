using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ThreatFramework.Core.Request;
using ThreatFramework.Core.Response;
using ThreatFramework.YamlFileGenerator.Contract;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YamlFileExporterController : ControllerBase
    {
        private readonly IYamlFileGenerator _yamlFileGenerator;
        private readonly ILogger<YamlFileExporterController> _logger;

        public YamlFileExporterController(IYamlFileGenerator yamlFileGenerator, ILogger<YamlFileExporterController> logger)
        {
            _yamlFileGenerator = yamlFileGenerator;
            _logger = logger;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateYamlFiles([FromBody] YamlGenerationRequest request)
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting YAML file generation request for path: {OutputPath} at {StartTime}", 
                request?.OutputPath, startTime);

            try
            {
                if (string.IsNullOrEmpty(request?.OutputPath))
                {
                    _logger.LogWarning("YAML generation request failed: Output path is required");
                    return BadRequest("Output path is required");
                }

                _logger.LogInformation("Calling YAML file generator for path: {OutputPath}", request.OutputPath);
                await _yamlFileGenerator.GenerateFilesToPathAsync(request.OutputPath);
                
                var endTime = DateTime.UtcNow;
                var elapsedTime = endTime - startTime;
                
                _logger.LogInformation("YAML file generation completed successfully for path: {OutputPath} in {ElapsedTime}ms", 
                    request.OutputPath, elapsedTime.TotalMilliseconds);
                
                return Ok(new YamlGenerationResponse
                {
                    Success = true,
                    Message = $"YAML files generated successfully in {elapsedTime.TotalSeconds:F2} seconds",
                });
            }
            catch (DirectoryNotFoundException dnfEx)
            {
                _logger.LogError(dnfEx, "Directory not found during YAML generation for path: {OutputPath}", request?.OutputPath);
                return BadRequest("Output path does not exist");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Access denied during YAML generation for path: {OutputPath}", request?.OutputPath);
                return StatusCode(403, "Access denied to the specified path");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during YAML generation for path: {OutputPath}. Error: {ErrorMessage}", 
                    request?.OutputPath, ex.Message);
                return StatusCode(500, $"Error generating YAML files: {ex.Message}");
            }
        }
    }
}

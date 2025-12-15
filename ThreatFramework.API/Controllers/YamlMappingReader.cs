using Microsoft.AspNetCore.Mvc;
using ThreatFramework.Infra.Contract.YamlRepository;
using ThreatFramework.Infrastructure.YamlRepository;
using ThreatModeler.TF.Core.Model.ComponentMapping;

namespace ThreatFramework.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YamlMappingReader : ControllerBase
    {
        private readonly IYamlComponentSRReader _componentSecurityRequirementReader;
        private readonly IYamlComponentPropertyReader _yamlComponentPropertyReader;
        private readonly IYamlComponentPropertyOptionReader _yamlComponentPropertyOptionReader;
        private readonly IYamlComponentPropertyOptionThreatReader _yamlComponentPropertyOptionThreatReader;
        private readonly IYamlCpoThreatSrReader _yamlComponentPropertyOptionThreatSrReader;
        private readonly IYamlComponentThreatReader _yamlComponentThreatReader;
        private readonly IYamlComponentThreatSRReader _yamlComponentThreatSRReader;
        private readonly IYamlThreatSrReader _yamlThreatSrReader;

        public YamlMappingReader(
            IYamlComponentSRReader componentSecurityRequirementReader,
            IYamlComponentPropertyOptionReader yamlPropertyOptionReader,
            IYamlComponentPropertyOptionThreatReader yamlPropertyOptionThreatReader,
            IYamlComponentPropertyReader yamlPropertyReader,
            IYamlComponentThreatReader yamlComponentThreatReader,
            IYamlComponentThreatSRReader yamlComponentThreatSRReader,
            IYamlCpoThreatSrReader yamlCpoThreatSrReader,
            IYamlThreatSrReader yamlThreatSrReader)
        {
            _componentSecurityRequirementReader = componentSecurityRequirementReader;
            _yamlComponentPropertyOptionReader = yamlPropertyOptionReader;
            _yamlComponentPropertyOptionThreatReader = yamlPropertyOptionThreatReader;
            _yamlComponentPropertyReader = yamlPropertyReader;
            _yamlComponentThreatReader = yamlComponentThreatReader;
            _yamlComponentThreatSRReader = yamlComponentThreatSRReader;
            _yamlComponentPropertyOptionThreatSrReader = yamlCpoThreatSrReader;
            _yamlThreatSrReader = yamlThreatSrReader;
        }

            [HttpGet("threat-sr")]
            public async Task<ActionResult<List<ComponentSecurityRequirementMapping>>> GetAllThreatSR(
                [FromQuery] string folderPath,
                CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return BadRequest("Folder path is required");
                }

                try
                {
                    var result = await _yamlThreatSrReader.GetAllAsync(folderPath, ct);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error reading YAML mappings: {ex.Message}");
                }
            }

            [HttpGet("component-property-option")]
            public async Task<ActionResult> GetAllComponentPropertyOption(
                [FromQuery] string folderPath,
                CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return BadRequest("Folder path is required");
                }

                try
                {
                    var result = await _yamlComponentPropertyOptionReader.GetAllAsync(folderPath, ct);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error reading YAML property options: {ex.Message}");
                }
            }

            [HttpGet("component-property-option-threat")]
            public async Task<ActionResult> GetAllComponentPropertyOptionThreat(
                [FromQuery] string folderPath,
                CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return BadRequest("Folder path is required");
                }

                try
                {
                    var result = await _yamlComponentPropertyOptionThreatReader.GetAllAsync(folderPath, ct);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error reading YAML property option threats: {ex.Message}");
                }
            }

            [HttpGet("component-property")]
            public async Task<ActionResult> GetAllComponentProperty(
                [FromQuery] string folderPath,
                CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return BadRequest("Folder path is required");
                }

                try
                {
                    var result = await _yamlComponentPropertyReader.GetAllAsync(folderPath, ct);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error reading YAML properties: {ex.Message}");
                }
            }

            [HttpGet("component-threat")]
            public async Task<ActionResult> GetAllComponentThreat(
                [FromQuery] string folderPath,
                CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return BadRequest("Folder path is required");
                }

                try
                {
                    var result = await _yamlComponentThreatReader.GetAllAsync(folderPath, ct);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error reading YAML component threats: {ex.Message}");
                }
            }

            [HttpGet("component-threat-sr")]
            public async Task<ActionResult> GetAllComponentThreatSR(
                [FromQuery] string folderPath,
                CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return BadRequest("Folder path is required");
                }

                try
                {
                    var result = await _yamlComponentThreatSRReader.GetAllAsync(folderPath, ct);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error reading YAML component threat SRs: {ex.Message}");
                }
            }

            [HttpGet("cpo-threat-sr")]
            public async Task<ActionResult> GetAllCpoThreatSr(
                [FromQuery] string folderPath,
                CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return BadRequest("Folder path is required");
                }

                try
                {
                    var result = await _yamlComponentPropertyOptionThreatSrReader.GetAllAsync(folderPath, ct);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error reading YAML CPO threat SRs: {ex.Message}");
                }
            }

            [HttpGet("threat-sr-general")]
            public async Task<ActionResult> GetAllThreatSr(
                [FromQuery] string folderPath,
                CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return BadRequest("Folder path is required");
                }

                try
                {
                    var result = await _yamlThreatSrReader.GetAllAsync(folderPath, ct);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error reading YAML threat SRs: {ex.Message}");
                }
            }

            [HttpGet("component-sr")]
            public async Task<ActionResult<List<ComponentSecurityRequirementMapping>>> GetAllComponentSR(
                [FromQuery] string folderPath,
                CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return BadRequest("Folder path is required");
                }

                try
                {
                    var result = await _componentSecurityRequirementReader.GetAllComponentSRAsync(folderPath, ct);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error reading YAML component security requirements: {ex.Message}");
                }
            }
        }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ThreatFramework.Core.Cache;
using ThreatFramework.YamlFileGenerator.Contract;
using ThreatModeler.TF.API.Controllers.Dtos;
using ThreatModeler.TF.Core.Model.CoreEntities;
using ThreatModeler.TF.Infra.Contract.Repository.CoreEntities;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class YamlExportsController : ControllerBase
{
    private readonly ILogger<YamlExportsController> _logger;
    private readonly IYamlFileGeneratorForClient _clientGenerator;
    private readonly IYamlFilesGeneratorForTRC _trcGenerator;
    private readonly PathOptions _exportOptions;
    private readonly ILibraryRepository _libraryRepository;
    private readonly List<Guid> defaultLibList = new()
    {
        Guid.Parse("EEF7DCF9-53BD-48E9-849D-21445EBAD101"),
        Guid.Parse("AE9A4C22-21DF-4455-82BB-279B74E6FB81")
    };


    public YamlExportsController(
        ILogger<YamlExportsController> logger,
        IYamlFileGeneratorForClient clientGenerator,
        IYamlFilesGeneratorForTRC trcGenerator,
        ILibraryRepository libraryRepository,
        IOptions<PathOptions> exportOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clientGenerator = clientGenerator ?? throw new ArgumentNullException(nameof(clientGenerator));
        _trcGenerator = trcGenerator ?? throw new ArgumentNullException(nameof(trcGenerator));
        _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
        _exportOptions = exportOptions?.Value ?? throw new ArgumentNullException(nameof(exportOptions));
    }

    [HttpPost("client")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateClientAsync(List<Guid> guids)
    {
        using (_logger.BeginScope("Action: GenerateClientYaml"))
        {
            if (!TryGetPath(_exportOptions.ClientOutput, "Client", out string path, out var errorResult))
            {
                return errorResult;
            }

            try
            {
                _logger.LogInformation("Starting Client YAML export to {Output}", path);

                // Assuming Client Generator signature hasn't changed yet. 
                // If it has, pass the push param here too.
                guids.AddRange(defaultLibList);
                await _clientGenerator.GenerateForLibraryIdsAsync(path, guids);

                _logger.LogInformation("Completed Client YAML export.");

                return Ok(new
                {
                    tenant = "Client",
                    outputPath = path,
                    status = "completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Client YAMLs.");
                return Problem(statusCode: 500, title: "Export Failed", detail: ex.Message);
            }
        }
    }

    [HttpPost("trc/readonly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateTrcReadOnlyAsync(
        [FromQuery] bool push = false,
        CancellationToken ct = default)
    {
        using (_logger.BeginScope("Action: GenerateTrcReadOnly Push={Push}", push))
        {
            if (!TryGetPath(_exportOptions.TrcOutput, "TRC", out string path, out var errorResult))
            {
                return errorResult;
            }

            try
            {
                _logger.LogInformation("Starting TRC ReadOnly export to {Output}", path);

                // Updated Contract Call
                await _trcGenerator.GenerateForReadOnlyLibraryAsync(path, pushToRemote: push);

                _logger.LogInformation("Completed TRC ReadOnly export.");

                return Ok(new
                {
                    tenant = "TRC",
                    scope = "ReadOnly",
                    outputPath = path,
                    pushedToGit = push,
                    status = "completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate TRC ReadOnly YAMLs.");
                return Problem(statusCode: 500, title: "Export Failed", detail: ex.Message);
            }
        }
    }

    [HttpPost("trc")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateTrcLibrariesAsync(
        [FromBody] LibraryExportRequest request,
        [FromQuery] bool push = false,
        CancellationToken ct = default)
    {
        using (_logger.BeginScope("Action: GenerateTrcLibraries Push={Push}", push))
        {
            if (!TryGetPath(_exportOptions.TrcOutput, "TRC", out string path, out var errorResult))
            {
                return errorResult;
            }

            if (request == null || !request.LibraryIds.Any())
            {
                _logger.LogWarning("No library IDs provided in request body.");
                return BadRequest(new { error = "Please provide a list of Library IDs." });
            }

            try
            {
                _logger.LogInformation("Starting TRC export for {Count} libraries to {Output}", request.LibraryIds.Count(), path);

                //request.LibraryIds.AddRange(defaultLibList);
                // Updated Contract Call

                var LibList = new List<Guid>
                {
                    Guid.Parse("EEF7DCF9-53BD-48E9-849D-21445EBAD101"),
                    Guid.Parse("AE9A4C22-21DF-4455-82BB-279B74E6FB81"),
                    Guid.Parse("F99B22A9-8676-432D-A365-103DD70FAA7E"),
                    Guid.Parse("3484F5E4-1185-4482-95DC-E1B88C81C976"),
                    Guid.Parse("7A402FB6-D4D3-4D75-BF5C-D163F9D7D7EF"),
                    Guid.Parse("C6A6997C-3FCB-4A2E-843D-E2C5F7F9DC96"),
                    Guid.Parse("D7CB903D-08DA-4AB6-A32E-40A18203CEC7"),
                    Guid.Parse("08927410-CDAF-4959-946E-DA5AE3A1496A"),
                    Guid.Parse("0359A988-F6F7-421A-8A87-E68AB918F79F"),
                    Guid.Parse("D3E6DEB0-4C0B-41D9-9F54-86F8D76946A2"),
                    Guid.Parse("E50269F4-B85F-43D5-85F0-4D0BC211A915"),


                };
                await _trcGenerator.GenerateForLibraryIdsAsync(path, LibList, pushToRemote: push);

                _logger.LogInformation("Completed TRC Library export.");

                return Ok(new
                {
                    tenant = "TRC",
                    scope = "SpecificLibraries",
                    count = request.LibraryIds.Count(),
                    outputPath = path,
                    pushedToGit = push,
                    status = "completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate TRC Library YAMLs.");
                return Problem(statusCode: 500, title: "Export Failed", detail: ex.Message);
            }
        }
    }

    [HttpPost("client/readonly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateClientReadOnlyAsync(CancellationToken ct = default)
    {
        using (_logger.BeginScope("Action: GenerateClientReadOnlyYaml"))
        {
            if (!TryGetPath(_exportOptions.ClientOutput, "Client", out string path, out var errorResult))
            {
                return errorResult!;
            }

            try
            {
                _logger.LogInformation("Starting Client ReadOnly YAML export to {Output}", path);

                var librariesCache = (await _libraryRepository.GetLibrariesCacheAsync())?.ToList()
                                     ?? new List<LibraryCache>();

                // IMPORTANT: adjust the property name below to match your LibraryCache model:
                // examples: IsReadOnly, IsReadonly, ReadOnly, IsLocked, etc.
                var readOnlyLibraryGuids = librariesCache
                    .Where(l => l != null && l.IsReadonly)     // <-- adjust if your prop name differs
                    .Select(l => l.Guid)
                    .Where(g => g != Guid.Empty)
                    .Distinct()
                    .ToList();

                if (!readOnlyLibraryGuids.Any())
                {
                    _logger.LogWarning("No read-only libraries found. Nothing to export.");
                    return Ok(new
                    {
                        tenant = "Client",
                        scope = "ReadOnly",
                        count = 0,
                        outputPath = path,
                        status = "no-libraries"
                    });
                }

                await _clientGenerator.GenerateForLibraryIdsAsync(path, readOnlyLibraryGuids);

                _logger.LogInformation("Completed Client ReadOnly YAML export for {Count} libraries.", readOnlyLibraryGuids.Count);

                return Ok(new
                {
                    tenant = "Client",
                    scope = "ReadOnly",
                    count = readOnlyLibraryGuids.Count,
                    outputPath = path,
                    status = "completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Client ReadOnly YAMLs.");
                return Problem(statusCode: 500, title: "Export Failed", detail: ex.Message);
            }
        }
    }



    // --- Private Helper (DRY) ---

    private bool TryGetPath(string? configuredPath, string contextName, out string validPath, out IActionResult? errorResult)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            _logger.LogError("{Context} output path is not configured in appsettings.", contextName);
            validPath = string.Empty;
            errorResult = Problem(statusCode: 400, title: "Configuration Error", detail: $"{contextName} output path not configured.");
            return false;
        }

        validPath = configuredPath;
        errorResult = null;
        return true;
    }
}

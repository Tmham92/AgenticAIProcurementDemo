using Microsoft.AspNetCore.Mvc;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Controllers;

/// <summary>
/// System-level diagnostics for the API, currently exposing local LLM provider health.
/// </summary>
[ApiController]
[Route("api/system")]
public class SystemController(IModelHealthCheckService modelHealthCheckService) : ControllerBase
{
    private readonly IModelHealthCheckService _modelHealthCheckService = modelHealthCheckService;

    /// <summary>
    /// Checks whether the local Ollama runtime is reachable and the configured model is
    /// loaded, returning connectivity status, model name, and round-trip latency.
    /// </summary>
    [HttpGet("ollama-status")]
    [ProducesResponseType(typeof(OllamaStatus), StatusCodes.Status200OK)]
    public async Task<ActionResult<OllamaStatus>> GetOllamaStatus()
    {
        var status = await _modelHealthCheckService.CheckStatusAsync();
        return Ok(status);
    }
}

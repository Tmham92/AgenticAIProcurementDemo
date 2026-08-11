using Microsoft.AspNetCore.Mvc;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Controllers;

/// <summary>
/// Exposes aggregated ProcOps dependency metrics, including the estimated number of
/// ProcOps tickets avoided, derived from recorded interaction history.
/// </summary>
[ApiController]
[Route("api/procops-metrics")]
public class ProcOpsMetricsController(IProcOpsDependencyService procOpsDependencyService) : ControllerBase
{
    private readonly IProcOpsDependencyService _procOpsDependencyService = procOpsDependencyService;

    [HttpGet]
    [ProducesResponseType(typeof(ProcOpsMetrics), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProcOpsMetrics>> GetMetrics()
    {
        return Ok(await _procOpsDependencyService.GetMetricsAsync());
    }
}

using Microsoft.AspNetCore.Mvc;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Controllers;

/// <summary>
/// Exposes the Procurement Control Tower dashboard: a single leadership-facing view
/// combining adoption, compliance, ProcOps dependency, policy deviations, top risks,
/// insights, and recommended actions.
/// </summary>
[ApiController]
[Route("api/controltower")]
public class ControlTowerController(
    IControlTowerService controlTowerService,
    IControlTowerAgentService controlTowerAgentService) : ControllerBase
{
    private readonly IControlTowerService _controlTowerService = controlTowerService;
    private readonly IControlTowerAgentService _controlTowerAgentService = controlTowerAgentService;

    [HttpGet]
    [ProducesResponseType(typeof(ControlTowerDashboard), StatusCodes.Status200OK)]
    public async Task<ActionResult<ControlTowerDashboard>> GetDashboard()
    {
        return Ok(await _controlTowerService.GetDashboardAsync());
    }

    /// <summary>
    /// Returns continuously-generated executive recommendations synthesized by the
    /// Control Tower Agent from adoption, compliance, country governance, and ProcOps
    /// dependency metrics.
    /// </summary>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetRecommendations()
    {
        return Ok(await _controlTowerAgentService.GetRecommendationsAsync());
    }
}

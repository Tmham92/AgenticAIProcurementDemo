using Microsoft.AspNetCore.Mvc;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Controllers;

/// <summary>
/// Exposes process discovery insights derived from analyzing all recorded interaction
/// history, surfacing patterns such as common categories/countries, recurring policy
/// deviations, low compliance hotspots, and unknown category usage.
/// </summary>
[ApiController]
[Route("api/process-discovery")]
public class ProcessDiscoveryController(IProcessDiscoveryInsightService processDiscoveryInsightService) : ControllerBase
{
    private readonly IProcessDiscoveryInsightService _processDiscoveryInsightService = processDiscoveryInsightService;

    /// <summary>
    /// Returns summary process discovery insights analyzed from all recorded interactions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProcessDiscoveryInsight>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProcessDiscoveryInsight>>> GetInsights()
    {
        var insights = await _processDiscoveryInsightService.DiscoverInsightsAsync();
        return Ok(insights);
    }
}

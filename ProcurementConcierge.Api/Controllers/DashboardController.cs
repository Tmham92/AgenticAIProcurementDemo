using Microsoft.AspNetCore.Mvc;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementHealthDashboard = ProcurementConcierge.Contracts.ProcurementHealthDashboard;

namespace ProcurementConcierge.Api.Controllers;

/// <summary>
/// Exposes aggregated adoption, compliance, and executive insight metrics derived from
/// request history, simulating the future Procurement Control Tower dashboard.
/// </summary>
[ApiController]
[Route("api/dashboard")]
public class DashboardController(
    IAdoptionInsightService adoptionInsightService,
    IComplianceInsightService complianceInsightService,
    IExecutiveInsightService executiveInsightService,
    IProcurementHealthService procurementHealthService) : ControllerBase
{
    private readonly IAdoptionInsightService _adoptionInsightService = adoptionInsightService;
    private readonly IComplianceInsightService _complianceInsightService = complianceInsightService;
    private readonly IExecutiveInsightService _executiveInsightService = executiveInsightService;
    private readonly IProcurementHealthService _procurementHealthService = procurementHealthService;

    /// <summary>
    /// Returns adoption metrics: requests per country/category, most common policy
    /// deviations, unknown categories, and the compliance score trend over time.
    /// </summary>
    [HttpGet("adoption")]
    [ProducesResponseType(typeof(AdoptionInsights), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdoptionInsights>> GetAdoptionInsights()
    {
        return Ok(await _adoptionInsightService.GetInsightsAsync());
    }

    /// <summary>
    /// Returns compliance metrics: average compliance score, level distribution, and
    /// average scores per category/country.
    /// </summary>
    [HttpGet("compliance")]
    [ProducesResponseType(typeof(ComplianceInsights), StatusCodes.Status200OK)]
    public async Task<ActionResult<ComplianceInsights>> GetComplianceInsights()
    {
        return Ok(await _complianceInsightService.GetInsightsAsync());
    }

    /// <summary>
    /// Returns a ranked list of executive-level findings synthesized from adoption,
    /// process discovery, and compliance data.
    /// </summary>
    [HttpGet("executive-insights")]
    [ProducesResponseType(typeof(ExecutiveInsights), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExecutiveInsights>> GetExecutiveInsights()
    {
        return Ok(await _executiveInsightService.GetInsightsAsync());
    }

    /// <summary>
    /// Returns the blended 0-100 Procurement Health Score dashboard (adoption, compliance,
    /// process quality, and request quality scores combined into an overall score).
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ProcurementHealthDashboard), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProcurementHealthDashboard>> GetHealth()
    {
        return Ok(await _procurementHealthService.GetHealthDashboardAsync());
    }
}

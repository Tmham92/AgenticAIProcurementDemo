using Microsoft.AspNetCore.Mvc;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Controllers;

/// <summary>
/// Exposes per-country procurement governance metrics derived from recorded interaction
/// history.
/// </summary>
[ApiController]
[Route("api/governance")]
public class GovernanceController(ICountryGovernanceService countryGovernanceService) : ControllerBase
{
    private readonly ICountryGovernanceService _countryGovernanceService = countryGovernanceService;

    /// <summary>
    /// Returns governance metrics for each country: total requests, average compliance
    /// and request quality scores, policy deviation counts, and top categories.
    /// </summary>
    [HttpGet("countries")]
    [ProducesResponseType(typeof(List<CountryGovernanceReport>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CountryGovernanceReport>>> GetCountryReports()
    {
        return Ok(await _countryGovernanceService.GetCountryReportsAsync());
    }
}

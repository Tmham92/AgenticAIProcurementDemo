using Microsoft.AspNetCore.Mvc;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Controllers;

/// <summary>
/// Exposes adoption intelligence findings: concrete, severity-ranked adoption problems
/// (repeated policy deviations, frequent supplier exceptions, low-compliance
/// countries/categories, high ProcOps dependency, common missing information) mined from
/// recorded interaction history.
/// </summary>
[ApiController]
[Route("api/adoption-intelligence")]
public class AdoptionIntelligenceController(IAdoptionIntelligenceService adoptionIntelligenceService) : ControllerBase
{
    private readonly IAdoptionIntelligenceService _adoptionIntelligenceService = adoptionIntelligenceService;

    /// <summary>
    /// Returns adoption findings ordered by severity (most severe first).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AdoptionFinding>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdoptionFinding>>> GetFindings()
    {
        var findings = await _adoptionIntelligenceService.GetFindingsAsync();
        return Ok(findings);
    }
}

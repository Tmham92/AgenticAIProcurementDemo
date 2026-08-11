using Microsoft.AspNetCore.Mvc;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Controllers;

[ApiController]
[Route("api/concierge")]
public class ConciergeController(IProcurementConciergeService conciergeService, ILogger<ConciergeController> logger) : ControllerBase
{
    private readonly IProcurementConciergeService _conciergeService = conciergeService;
    private readonly ILogger<ConciergeController> _logger = logger;

    /// <summary>
    /// Accepts a natural language procurement request and returns procurement guidance,
    /// including compliance risk, required approvals, policy deviations, and a recommended
    /// next action. This endpoint does NOT create a purchase order or procurement transaction -
    /// it exists to guide the user towards the correct procurement process.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProcurementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProcurementResponse>> Post([FromBody] ProcurementRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Request message must not be empty.");
        }

        _logger.LogInformation("Received procurement request: {Message}", request.Message);

        var response = await _conciergeService.ProcessRequestAsync(request);

        return Ok(response);
    }
}

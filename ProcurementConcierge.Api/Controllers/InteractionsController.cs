using Microsoft.AspNetCore.Mvc;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Controllers;

/// <summary>
/// Exposes recorded interaction insights collected from every analyzed procurement request.
/// </summary>
[ApiController]
[Route("api/interactions")]
public class InteractionsController(IInteractionLoggingService interactionLoggingService) : ControllerBase
{
    private readonly IInteractionLoggingService _interactionLoggingService = interactionLoggingService;

    /// <summary>
    /// Returns all recorded interactions, most recent first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<InteractionRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<InteractionRecord>>> GetAll()
    {
        var interactions = await _interactionLoggingService.GetAllAsync();
        return Ok(interactions);
    }
}

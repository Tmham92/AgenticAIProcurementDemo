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
    IControlTowerAgentService controlTowerAgentService,
    IControlTowerChatAgent controlTowerChatAgent) : ControllerBase
{
    private readonly IControlTowerService _controlTowerService = controlTowerService;
    private readonly IControlTowerAgentService _controlTowerAgentService = controlTowerAgentService;
    private readonly IControlTowerChatAgent _controlTowerChatAgent = controlTowerChatAgent;

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

    /// <summary>
    /// Allows procurement leadership to ask a natural language question about adoption,
    /// governance, compliance, or process performance, grounded in real organizational data.
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ControlTowerAnswer), StatusCodes.Status200OK)]
    public async Task<ActionResult<ControlTowerAnswer>> Chat([FromBody] ControlTowerQuestion request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question must not be empty.");
        }

        var answer = await _controlTowerChatAgent.AskAsync(request.Question);
        return Ok(answer);
    }

    /// <summary>
    /// Returns persisted Control Tower Chat conversation history, most recent first.
    /// </summary>
    [HttpGet("chat/history")]
    [ProducesResponseType(typeof(List<ControlTowerConversationEntry>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ControlTowerConversationEntry>>> GetChatHistory()
    {
        var history = await _controlTowerChatAgent.GetHistoryAsync();
        var entries = history.Select(c => new ControlTowerConversationEntry
        {
            Id = c.Id,
            Question = c.Question,
            Answer = c.Answer,
            Timestamp = c.Timestamp
        }).ToList();

        return Ok(entries);
    }
}


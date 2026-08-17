using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Interprets a procurement leadership question in natural language, gathers grounding
/// data from the Control Tower's organizational data sources, and generates an
/// executive-level answer with citations, recommended actions, and a confidence score.
/// </summary>
public interface IControlTowerChatAgent
{
    Task<ControlTowerAnswer> AskAsync(string question);

    /// <summary>
    /// Returns the persisted conversation history, most recent first.
    /// </summary>
    Task<List<ControlTowerConversation>> GetHistoryAsync();
}

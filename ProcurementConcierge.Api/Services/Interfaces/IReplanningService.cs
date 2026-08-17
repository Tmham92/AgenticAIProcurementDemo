using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Decides whether Dynamic Replanning should generate and execute an additional follow-up
/// plan of agents after reflection, based on the current run's working memory and the
/// reflection outcome (missing information, incomplete goals, low confidence, etc.).
/// Enforces the maximum iteration budget so the orchestrator never loops indefinitely.
/// </summary>
public interface IReplanningService
{
    ReplanDecision Decide(Agents.AgentRunContext context, ReflectionResult reflection);
}

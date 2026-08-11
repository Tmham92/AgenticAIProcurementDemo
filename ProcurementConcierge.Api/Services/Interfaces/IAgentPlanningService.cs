using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Uses Azure OpenAI (via Semantic Kernel) to determine which agents are required to
/// satisfy a user's procurement request, in what order, and why. Falls back to a
/// deterministic keyword-based plan when the model is unavailable or its response cannot
/// be parsed, mirroring the fallback pattern used by <see cref="IRequestAnalysisService"/>.
/// </summary>
public interface IAgentPlanningService
{
    Task<AgentPlan> CreatePlanAsync(string userRequest);
}

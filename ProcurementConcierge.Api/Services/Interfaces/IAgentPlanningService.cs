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

    /// <summary>
    /// Builds a deterministic follow-up plan (Dynamic Replanning) from an explicit,
    /// already-decided list of agent names (e.g. from <see cref="IReplanningService"/>),
    /// preserving their order as sequential priorities. Used when the agents required to
    /// complete the goal are already known, avoiding a redundant LLM planning round-trip.
    /// </summary>
    AgentPlan CreateFollowUpPlan(IReadOnlyList<string> agentNames, string goal);
}

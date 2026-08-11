using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Central agent orchestrator: receives a user's goal, generates an execution plan,
/// selects and executes the required agents in order, collects results, reflects on the
/// outcome, decides whether human intervention is required, and returns the complete
/// execution result including the full reasoning/execution timeline.
/// </summary>
public interface IAgentOrchestrator
{
    Task<AgentExecutionResult> RunAsync(AgentGoal goal);
}

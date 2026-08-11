namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Contract implemented by every specialized agent that can be selected and executed by
/// the <see cref="IAgentOrchestrator"/>. Each agent reads whatever it needs from the
/// shared <see cref="AgentRunContext.WorkingMemory"/> and writes its outputs back into it.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// The unique name used by the planner to select this agent (must match the agent
    /// names the <see cref="Services.Interfaces.IAgentPlanningService"/> can plan for).
    /// </summary>
    string Name { get; }

    Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context);
}

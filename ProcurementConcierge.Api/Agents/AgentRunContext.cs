using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// The mutable run-time context shared across all agents during a single orchestrated
/// execution: the original goal, the accumulating execution timeline, and a short-term
/// working memory that agents use to pass structured data (category, country, spend,
/// policy, compliance results, etc.) to one another without tight coupling.
/// </summary>
public class AgentRunContext
{
    public Guid ExecutionId { get; init; } = Guid.NewGuid();
    public AgentGoal Goal { get; init; } = new();
    public List<ExecutionStep> Steps { get; } = [];
    public Dictionary<string, object> WorkingMemory { get; } = [];

    /// <summary>
    /// The current Dynamic Replanning iteration (1-based). Incremented by the orchestrator
    /// each time a follow-up plan is executed, and stamped onto every step added while it
    /// is in effect so the UI can group the execution trace by iteration.
    /// </summary>
    public int CurrentIteration { get; set; } = 1;

    public void AddStep(int stepNumber, string agentName, string reason, string outcome)
    {
        Steps.Add(new ExecutionStep
        {
            StepNumber = stepNumber,
            AgentName = agentName,
            Reason = reason,
            Outcome = outcome,
            Timestamp = DateTime.UtcNow,
            IterationNumber = CurrentIteration
        });
    }

    public T? GetMemory<T>(string key)
    {
        return WorkingMemory.TryGetValue(key, out var value) && value is T typed ? typed : default;
    }

    public void SetMemory(string key, object value)
    {
        WorkingMemory[key] = value;
    }
}

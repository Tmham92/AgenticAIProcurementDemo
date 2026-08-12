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

    public void AddStep(int stepNumber, string agentName, string reason, string outcome)
    {
        Steps.Add(new ExecutionStep
        {
            StepNumber = stepNumber,
            AgentName = agentName,
            Reason = reason,
            Outcome = outcome,
            Timestamp = DateTime.UtcNow
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

namespace ProcurementConcierge.Contracts;

/// <summary>
/// Represents the user's high-level goal submitted to the Agent Orchestrator.
/// </summary>
public class AgentGoal
{
    public string UserRequest { get; set; } = string.Empty;
    public string GoalDescription { get; set; } = string.Empty;
}

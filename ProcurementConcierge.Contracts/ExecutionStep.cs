namespace ProcurementConcierge.Contracts;

/// <summary>
/// A single entry in the agent orchestrator's execution timeline, describing which agent
/// ran, why the planner selected it, what happened, and when.
/// </summary>
public class ExecutionStep
{
    public int StepNumber { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The Dynamic Replanning iteration (1-based) during which this agent ran, so the UI
    /// can group the execution trace by iteration.
    /// </summary>
    public int IterationNumber { get; set; } = 1;
}

namespace ProcurementConcierge.Contracts;

/// <summary>
/// The final outcome of an orchestrated agent execution run, including whether the goal
/// was achieved, whether a human must intervene, the full execution timeline, and the
/// collected outputs from all executed agents.
/// </summary>
public class AgentExecutionResult
{
    public bool Success { get; set; }
    public string Summary { get; set; } = string.Empty;
    public bool HumanInterventionRequired { get; set; }
    public List<ExecutionStep> Steps { get; set; } = [];
    public Dictionary<string, object> Outputs { get; set; } = [];

    /// <summary>
    /// The total number of Dynamic Replanning iterations executed for this run (starts at 1).
    /// </summary>
    public int Iterations { get; set; } = 1;

    /// <summary>
    /// Auditable trace of every replanning decision made during this run, including which
    /// iteration triggered it, why, and which agents were added to the follow-up plan.
    /// </summary>
    public List<ReplanningHistory> ReplanningHistory { get; set; } = [];
}

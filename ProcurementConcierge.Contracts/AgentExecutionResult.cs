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
    public List<ExecutionStep> Steps { get; set; } = new();
    public Dictionary<string, object> Outputs { get; set; } = new();
}

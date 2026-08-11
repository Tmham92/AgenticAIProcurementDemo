namespace ProcurementConcierge.Contracts;

/// <summary>
/// A single planned agent invocation produced by the AI planner, including the reason it
/// was selected and its relative priority/order in the plan.
/// </summary>
public class PlannedTask
{
    public string AgentName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int Priority { get; set; }
}

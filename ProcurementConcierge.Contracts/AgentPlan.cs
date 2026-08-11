namespace ProcurementConcierge.Contracts;

/// <summary>
/// The AI-generated (or heuristically-derived) execution plan: the overall goal plus the
/// ordered set of agents required to satisfy it.
/// </summary>
public class AgentPlan
{
    public string Goal { get; set; } = string.Empty;
    public List<PlannedTask> Tasks { get; set; } = new();
}

namespace ProcurementConcierge.Contracts;

/// <summary>
/// An auditable record of a single replanning decision made during an orchestrated
/// execution run: which iteration triggered it, why replanning was required, and which
/// additional agents were added to the follow-up plan.
/// </summary>
public class ReplanningHistory
{
    public int Iteration { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> AddedAgents { get; set; } = [];
}

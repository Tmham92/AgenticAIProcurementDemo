namespace ProcurementConcierge.Contracts;

/// <summary>
/// The Replanning Service's decision on whether the current execution run must continue
/// with an additional follow-up plan of agents before the user's goal can be considered
/// achieved (or escalation becomes required).
/// </summary>
public class ReplanDecision
{
    public bool ReplanRequired { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int CurrentIteration { get; set; }
    public int MaxIterations { get; set; }
    public List<string> AdditionalTasks { get; set; } = [];
}

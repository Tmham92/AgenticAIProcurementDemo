namespace ProcurementConcierge.Contracts;

/// <summary>
/// The Escalation Agent's decision on how much human involvement a request requires,
/// together with the reason for that decision.
/// </summary>
public class EscalationDecision
{
    public EscalationLevel Level { get; set; }
    public string Reason { get; set; } = string.Empty;
}

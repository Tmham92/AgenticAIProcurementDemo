namespace ProcurementConcierge.Api.Models;

/// <summary>
/// Human-in-the-loop escalation requirement derived from the compliance score.
/// </summary>
public class EscalationInfo
{
    public string Level { get; set; } = "Automatic";
    public string Reason { get; set; } = string.Empty;
}

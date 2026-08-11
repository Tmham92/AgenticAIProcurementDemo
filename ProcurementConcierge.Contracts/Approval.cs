namespace ProcurementConcierge.Contracts;

/// <summary>
/// Simulated Coupa approval step in a requisition's approval route.
/// </summary>
public class Approval
{
    public int Step { get; set; }
    public string ApproverRole { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}

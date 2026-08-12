namespace ProcurementConcierge.Contracts;

/// <summary>
/// Simulated Coupa requisition draft, representing what would be created in Coupa once a
/// real integration exists. Statuses progress through a simple simulated lifecycle.
/// </summary>
public class Requisition
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal EstimatedSpend { get; set; }
    public string? SupplierName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<string> ApprovalRoute { get; set; } = [];
}

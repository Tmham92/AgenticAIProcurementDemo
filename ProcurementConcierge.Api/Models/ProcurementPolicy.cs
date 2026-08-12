namespace ProcurementConcierge.Api.Models;

/// <summary>
/// Represents the procurement policy configuration for a given category,
/// as loaded from Data/policies.json.
/// </summary>
public class ProcurementPolicy
{
    public string Category { get; set; } = string.Empty;
    public List<string> PreferredSuppliers { get; set; } = [];
    public decimal DirectorApprovalThreshold { get; set; }
    public decimal CpoApprovalThreshold { get; set; }
}

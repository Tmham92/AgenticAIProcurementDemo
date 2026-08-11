namespace ProcurementConcierge.Api.Models;

/// <summary>
/// Input used to create a simulated Coupa draft requisition.
/// </summary>
public class CreateRequisitionRequest
{
    public string Category { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal EstimatedSpend { get; set; }
    public string? PreferredSupplierName { get; set; }
}

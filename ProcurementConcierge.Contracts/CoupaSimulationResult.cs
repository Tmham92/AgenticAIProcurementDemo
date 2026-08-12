namespace ProcurementConcierge.Contracts;

/// <summary>
/// Simulated Coupa integration result attached to a Procurement Concierge response,
/// showing what would happen if this request were sent to Coupa: a draft requisition,
/// its approval route, and the preferred suppliers considered.
/// </summary>
public class CoupaSimulationResult
{
    public List<Supplier> PreferredSuppliers { get; set; } = [];
    public Requisition DraftRequisition { get; set; } = new();
    public List<Approval> ApprovalRoute { get; set; } = [];
}

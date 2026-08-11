using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Simulates a future Coupa integration (supplier lookup, requisition creation, approval
/// routing, and status tracking) without requiring a real connection to Coupa.
/// </summary>
public interface ICoupaSimulationService
{
    Task<List<Supplier>> GetPreferredSuppliers(string category);

    Task<Requisition> CreateDraftRequisition(CreateRequisitionRequest request);

    Task<List<Approval>> GetApprovalRoute(string category, decimal amount);

    Requisition? GetRequisitionStatus(string id);
}

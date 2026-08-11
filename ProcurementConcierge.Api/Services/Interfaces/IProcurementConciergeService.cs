using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Orchestrates the end-to-end procurement concierge agentic flow:
/// analysis -> category identification -> policy retrieval -> approval determination -> recommendation.
/// </summary>
public interface IProcurementConciergeService
{
    Task<ProcurementResponse> ProcessRequestAsync(ProcurementRequest request);
}

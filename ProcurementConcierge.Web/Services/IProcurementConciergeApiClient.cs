using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Web.Services;

/// <summary>
/// Client-side abstraction for interacting with the Procurement Concierge API. Implemented by
/// <see cref="ProcurementConciergeApiClient"/> which calls the separately hosted API project.
/// </summary>
public interface IProcurementConciergeApiClient
{
    Task<ProcurementResponse> ProcessRequestAsync(ProcurementRequest request);

    Task<AdoptionInsights> GetAdoptionInsightsAsync();

    Task<ComplianceInsights> GetComplianceInsightsAsync();

    Task<ExecutiveInsights> GetExecutiveInsightsAsync();

    Task<List<InteractionRecord>> GetInteractionsAsync();

    Task<List<ProcessDiscoveryInsight>> GetProcessDiscoveryInsightsAsync();

    Task<List<AdoptionFinding>> GetAdoptionFindingsAsync();

    Task<ControlTowerAnswer> AskControlTowerAsync(ControlTowerQuestion question);

    Task<List<ControlTowerConversationEntry>> GetControlTowerHistoryAsync();
}

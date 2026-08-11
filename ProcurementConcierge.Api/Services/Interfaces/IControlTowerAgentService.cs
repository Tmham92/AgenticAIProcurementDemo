using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Continuously synthesizes executive-level recommendations for the Procurement Control
/// Tower from adoption, compliance, country governance, and ProcOps dependency metrics.
/// </summary>
public interface IControlTowerAgentService
{
    Task<List<string>> GetRecommendationsAsync();
}

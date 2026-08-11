using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Generates adoption metrics (requests per country/category, common deviations,
/// unknown categories, compliance trend) from the request history store.
/// </summary>
public interface IAdoptionInsightService
{
    Task<AdoptionInsights> GetInsightsAsync();
}

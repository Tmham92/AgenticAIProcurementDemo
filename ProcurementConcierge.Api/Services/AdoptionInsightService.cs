using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Aggregates persisted interaction records into adoption metrics to power the adoption dashboard.
/// </summary>
public class AdoptionInsightService(IInteractionLoggingService interactionLoggingService) : IAdoptionInsightService
{
    private readonly IInteractionLoggingService _interactionLoggingService = interactionLoggingService;

    public async Task<AdoptionInsights> GetInsightsAsync()
    {
        var records = await _interactionLoggingService.GetAllAsync();

        var insights = new AdoptionInsights
        {
            TotalRequests = records.Count,
            RequestsPerCountry = records
                .GroupBy(r => r.Country)
                .ToDictionary(g => g.Key, g => g.Count()),
            RequestsPerCategory = records
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.Count()),
            MostCommonDeviations = records
                .SelectMany(r => r.DeviationTypes)
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count()),
            UnknownCategoryCount = records.Count(r =>
                string.Equals(r.Category, "Unknown", StringComparison.OrdinalIgnoreCase)),
            ComplianceTrend = [.. records
                .OrderBy(r => r.Timestamp)
                .Select(r => new ComplianceTrendPoint { Timestamp = r.Timestamp, ComplianceScore = r.ComplianceScore })]
        };

        return insights;
    }
}

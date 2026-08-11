using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Aggregates persisted interaction records into compliance metrics to power the compliance dashboard.
/// </summary>
public class ComplianceInsightService(IInteractionLoggingService interactionLoggingService) : IComplianceInsightService
{
    private readonly IInteractionLoggingService _interactionLoggingService = interactionLoggingService;

    public async Task<ComplianceInsights> GetInsightsAsync()
    {
        var records = await _interactionLoggingService.GetAllAsync();

        if (records.Count == 0)
        {
            return new ComplianceInsights();
        }

        return new ComplianceInsights
        {
            AverageComplianceScore = records.Average(r => r.ComplianceScore),
            ComplianceLevelDistribution = records
                .GroupBy(r => r.ComplianceLevel)
                .ToDictionary(g => g.Key, g => g.Count()),
            AverageComplianceScorePerCategory = records
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.Average(r => r.ComplianceScore)),
            AverageComplianceScorePerCountry = records
                .GroupBy(r => r.Country)
                .ToDictionary(g => g.Key, g => g.Average(r => r.ComplianceScore))
        };
    }
}

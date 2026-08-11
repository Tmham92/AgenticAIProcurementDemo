using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;
using InteractionRecord = ProcurementConcierge.Api.Models.InteractionRecord;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Analyzes recorded interaction history grouped by country, surfacing total requests,
/// average compliance/request quality scores, policy deviation counts, and the most
/// common categories requested from each country.
/// </summary>
public class CountryGovernanceService(IInteractionLoggingService interactionLoggingService) : ICountryGovernanceService
{
    private const int TopCategoryCount = 3;

    private readonly IInteractionLoggingService _interactionLoggingService = interactionLoggingService;

    public async Task<List<CountryGovernanceReport>> GetCountryReportsAsync()
    {
        var records = await _interactionLoggingService.GetAllAsync();

        return [.. records
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Country) ? "Unknown" : r.Country)
            .Select(group => new CountryGovernanceReport
            {
                Country = group.Key,
                TotalRequests = group.Count(),
                ComplianceScore = (int)Math.Round(group.Average(r => r.ComplianceScore)),
                RequestQualityScore = (int)Math.Round(group.Average(CalculateRequestQualityScore)),
                PolicyDeviationCount = group.Count(r => r.PolicyDeviation),
                TopCategories = [.. group
                    .GroupBy(r => r.Category)
                    .OrderByDescending(g => g.Count())
                    .Take(TopCategoryCount)
                    .Select(g => g.Key)]
            })
            .OrderByDescending(report => report.TotalRequests)];
    }

    private static int CalculateRequestQualityScore(InteractionRecord record) =>
        RequestQualityHeuristics.CalculateScore(record.Category, record.Country, record.EstimatedSpend, record.OriginalRequest);
}

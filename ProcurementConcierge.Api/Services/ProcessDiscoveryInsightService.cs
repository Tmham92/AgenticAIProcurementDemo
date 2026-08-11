using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Analyzes recorded <see cref="InteractionRecord"/> history to surface process discovery
/// insights - patterns indicating where users struggle to submit complete, compliant
/// procurement requests (common categories/countries, recurring policy deviations, low
/// compliance hotspots, and unknown category usage).
/// </summary>
public class ProcessDiscoveryInsightService(IInteractionLoggingService interactionLoggingService) : IProcessDiscoveryInsightService
{
    private const int LowComplianceThreshold = 60;
    private const int MinimumSampleSizeForCategoryOrCountryInsight = 2;

    private readonly IInteractionLoggingService _interactionLoggingService = interactionLoggingService;

    public async Task<List<ProcessDiscoveryInsight>> DiscoverInsightsAsync()
    {
        var records = await _interactionLoggingService.GetAllAsync();
        var insights = new List<ProcessDiscoveryInsight>();

        if (records.Count == 0)
        {
            return insights;
        }

        AddMostCommonCategoryInsights(records, insights);
        AddMostCommonCountryInsights(records, insights);
        AddMostCommonPolicyDeviationInsights(records, insights);
        AddLowestComplianceCategoryInsights(records, insights);
        AddLowestComplianceCountryInsights(records, insights);
        AddUnknownCategoryInsight(records, insights);

        return insights;
    }

    private static void AddMostCommonCategoryInsights(List<InteractionRecord> records, List<ProcessDiscoveryInsight> insights)
    {
        var topCategory = records
            .GroupBy(r => r.Category)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (topCategory is null)
        {
            return;
        }

        var percentage = Percentage(topCategory.Count(), records.Count);
        insights.Add(new ProcessDiscoveryInsight
        {
            Finding = $"\"{topCategory.Key}\" is the most common request category, accounting for {percentage}% of requests.",
            Severity = "Info",
            Recommendation = $"Ensure procurement policy and preferred supplier lists for \"{topCategory.Key}\" are up to date, since this category drives the highest request volume."
        });
    }

    private static void AddMostCommonCountryInsights(List<InteractionRecord> records, List<ProcessDiscoveryInsight> insights)
    {
        var topCountry = records
            .GroupBy(r => r.Country)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (topCountry is null)
        {
            return;
        }

        var percentage = Percentage(topCountry.Count(), records.Count);
        insights.Add(new ProcessDiscoveryInsight
        {
            Finding = $"{topCountry.Key} is the most common request origin, accounting for {percentage}% of requests.",
            Severity = "Info",
            Recommendation = $"Confirm country-specific guidance for {topCountry.Key} is current and easily discoverable by requesters."
        });
    }

    private static void AddMostCommonPolicyDeviationInsights(List<InteractionRecord> records, List<ProcessDiscoveryInsight> insights)
    {
        var deviatedRecords = records.Where(r => r.PolicyDeviation).ToList();
        if (deviatedRecords.Count == 0)
        {
            return;
        }

        var overallPercentage = Percentage(deviatedRecords.Count, records.Count);
        insights.Add(new ProcessDiscoveryInsight
        {
            Finding = $"Policy deviations occur in {overallPercentage}% of all requests.",
            Severity = overallPercentage >= 30 ? "High" : "Medium",
            Recommendation = "Review the most frequent deviation reasons and consider clarifying policy or expanding preferred supplier coverage."
        });

        var topDeviationCategory = deviatedRecords
            .GroupBy(r => r.Category)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault(g => g.Count() >= MinimumSampleSizeForCategoryOrCountryInsight);

        if (topDeviationCategory is not null)
        {
            var categoryDeviationRate = Percentage(topDeviationCategory.Count(),
                records.Count(r => string.Equals(r.Category, topDeviationCategory.Key, StringComparison.OrdinalIgnoreCase)));

            var nonPreferredSupplierHint = topDeviationCategory
                .Any(r => r.Recommendation.Contains("preferred supplier", StringComparison.OrdinalIgnoreCase));

            var finding = nonPreferredSupplierHint
                ? $"Non-preferred supplier requests occur frequently within {topDeviationCategory.Key}."
                : $"Policy deviations occur frequently within {topDeviationCategory.Key} requests ({categoryDeviationRate}% deviation rate).";

            insights.Add(new ProcessDiscoveryInsight
            {
                Finding = finding,
                Severity = categoryDeviationRate >= 50 ? "High" : "Medium",
                Recommendation = $"Investigate why {topDeviationCategory.Key} requests frequently deviate from policy and consider targeted requester guidance or expanded preferred supplier options."
            });
        }
    }

    private static void AddLowestComplianceCategoryInsights(List<InteractionRecord> records, List<ProcessDiscoveryInsight> insights)
    {
        var lowComplianceCategories = records
            .GroupBy(r => r.Category)
            .Where(g => g.Count() >= MinimumSampleSizeForCategoryOrCountryInsight)
            .Select(g => new { Category = g.Key, AverageScore = g.Average(r => r.ComplianceScore) })
            .Where(g => g.AverageScore < LowComplianceThreshold)
            .OrderBy(g => g.AverageScore)
            .ToList();

        foreach (var group in lowComplianceCategories)
        {
            insights.Add(new ProcessDiscoveryInsight
            {
                Finding = $"{group.Category} requests have an average compliance score of {Math.Round(group.AverageScore)}, below the {LowComplianceThreshold} threshold.",
                Severity = group.AverageScore < 40 ? "High" : "Medium",
                Recommendation = $"Provide additional guidance or approval controls for {group.Category} requests to raise compliance scores above {LowComplianceThreshold}."
            });
        }
    }

    private static void AddLowestComplianceCountryInsights(List<InteractionRecord> records, List<ProcessDiscoveryInsight> insights)
    {
        var lowComplianceCountries = records
            .GroupBy(r => r.Country)
            .Where(g => g.Count() >= MinimumSampleSizeForCategoryOrCountryInsight)
            .Select(g => new { Country = g.Key, AverageScore = g.Average(r => r.ComplianceScore) })
            .Where(g => g.AverageScore < LowComplianceThreshold)
            .OrderBy(g => g.AverageScore)
            .ToList();

        foreach (var group in lowComplianceCountries)
        {
            insights.Add(new ProcessDiscoveryInsight
            {
                Finding = $"Requests from {group.Country} have an average compliance score of {Math.Round(group.AverageScore)}, below the {LowComplianceThreshold} threshold.",
                Severity = group.AverageScore < 40 ? "High" : "Medium",
                Recommendation = $"Review country-specific guidance and approval workflows for {group.Country} to improve compliance outcomes."
            });
        }

        // Combined category+country hotspots, e.g. "Marketing requests in France have a compliance score below 60."
        var lowComplianceCategoryCountryPairs = records
            .GroupBy(r => (r.Category, r.Country))
            .Where(g => g.Count() >= MinimumSampleSizeForCategoryOrCountryInsight)
            .Select(g => new { g.Key.Category, g.Key.Country, AverageScore = g.Average(r => r.ComplianceScore) })
            .Where(g => g.AverageScore < LowComplianceThreshold)
            .OrderBy(g => g.AverageScore)
            .ToList();

        foreach (var pair in lowComplianceCategoryCountryPairs)
        {
            insights.Add(new ProcessDiscoveryInsight
            {
                Finding = $"{pair.Category} requests in {pair.Country} have a compliance score below {LowComplianceThreshold}.",
                Severity = pair.AverageScore < 40 ? "High" : "Medium",
                Recommendation = $"Prioritize targeted coaching or policy clarification for {pair.Category} requests originating from {pair.Country}."
            });
        }
    }

    private static void AddUnknownCategoryInsight(List<InteractionRecord> records, List<ProcessDiscoveryInsight> insights)
    {
        var unknownCount = records.Count(r => string.Equals(r.Category, "Unknown", StringComparison.OrdinalIgnoreCase));
        if (unknownCount == 0)
        {
            return;
        }

        var percentage = Percentage(unknownCount, records.Count);
        insights.Add(new ProcessDiscoveryInsight
        {
            Finding = $"Unknown categories appear in {percentage}% of requests.",
            Severity = percentage >= 20 ? "High" : percentage >= 10 ? "Medium" : "Low",
            Recommendation = "Expand category detection (keywords/examples) or prompt requesters to select from a defined category list to reduce unknown classifications."
        });
    }

    private static int Percentage(int part, int total)
    {
        if (total == 0)
        {
            return 0;
        }

        return (int)Math.Round(part * 100.0 / total);
    }
}

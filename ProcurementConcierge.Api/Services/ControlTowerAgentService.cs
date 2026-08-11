using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// The Control Tower Agent: continuously analyzes adoption metrics, compliance metrics,
/// country governance patterns, and ProcOps dependency to generate concrete, executive-
/// readable recommendations for procurement leadership.
/// </summary>
public class ControlTowerAgentService(
    IAdoptionInsightService adoptionInsightService,
    IComplianceInsightService complianceInsightService,
    ICountryGovernanceService countryGovernanceService,
    IProcOpsDependencyService procOpsDependencyService) : IControlTowerAgentService
{
    private readonly IAdoptionInsightService _adoptionInsightService = adoptionInsightService;
    private readonly IComplianceInsightService _complianceInsightService = complianceInsightService;
    private readonly ICountryGovernanceService _countryGovernanceService = countryGovernanceService;
    private readonly IProcOpsDependencyService _procOpsDependencyService = procOpsDependencyService;

    public async Task<List<string>> GetRecommendationsAsync()
    {
        var recommendations = new List<string>();

        var adoptionInsights = await _adoptionInsightService.GetInsightsAsync();
        var complianceInsights = await _complianceInsightService.GetInsightsAsync();
        var countryReports = await _countryGovernanceService.GetCountryReportsAsync();
        var procOpsMetrics = await _procOpsDependencyService.GetMetricsAsync();

        if (adoptionInsights.TotalRequests == 0)
        {
            recommendations.Add("No procurement requests have been processed yet - no executive recommendations are available.");
            return recommendations;
        }

        // Country/category compliance gaps, e.g. "Marketing requests in France have a 40%
        // lower compliance score than average."
        foreach (var report in countryReports)
        {
            if (complianceInsights.AverageComplianceScore <= 0)
            {
                continue;
            }

            var gap = (complianceInsights.AverageComplianceScore - report.ComplianceScore) / complianceInsights.AverageComplianceScore * 100;
            if (gap >= 20 && report.TopCategories.Count > 0)
            {
                recommendations.Add(
                    $"{report.TopCategories.First()} requests in {report.Country} have a {gap:F0}% lower compliance score than average.");
            }
        }

        // Preferred supplier adoption trend, derived from most common deviation types.
        if (adoptionInsights.MostCommonDeviations.TryGetValue("Non-Preferred Supplier", out var nonPreferredCount)
            && nonPreferredCount > 0)
        {
            var nonPreferredRate = nonPreferredCount * 100.0 / adoptionInsights.TotalRequests;
            if (nonPreferredRate >= 20)
            {
                recommendations.Add($"Preferred supplier adoption is decreasing - {nonPreferredRate:F0}% of requests bypass preferred suppliers.");
            }
        }

        // ProcOps dependency concentration by category.
        if (procOpsMetrics.InterventionReasons.Count > 0)
        {
            var topReason = procOpsMetrics.InterventionReasons.OrderByDescending(kvp => kvp.Value).First();
            var concentration = topReason.Value * 100.0 / Math.Max(procOpsMetrics.InterventionRequiredCount, 1);
            if (concentration >= 40)
            {
                recommendations.Add($"ProcOps dependency is concentrated in {topReason.Key} ({concentration:F0}% of interventions).");
            }
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Procurement adoption and compliance metrics are healthy across all monitored dimensions.");
        }

        return recommendations;
    }
}

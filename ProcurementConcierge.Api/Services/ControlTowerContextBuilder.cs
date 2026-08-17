using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Aggregates Adoption Intelligence, Compliance Insights, Country Governance, Process
/// Discovery, and Executive Insights into a single <see cref="ControlTowerContext"/> so the
/// Control Tower Chat Agent can ground its answers in real organizational data rather than
/// inventing metrics.
/// </summary>
public class ControlTowerContextBuilder(
    IAdoptionIntelligenceService adoptionIntelligenceService,
    IComplianceInsightService complianceInsightService,
    ICountryGovernanceService countryGovernanceService,
    IProcessDiscoveryInsightService processDiscoveryInsightService,
    IExecutiveInsightService executiveInsightService) : IControlTowerContextBuilder
{
    private readonly IAdoptionIntelligenceService _adoptionIntelligenceService = adoptionIntelligenceService;
    private readonly IComplianceInsightService _complianceInsightService = complianceInsightService;
    private readonly ICountryGovernanceService _countryGovernanceService = countryGovernanceService;
    private readonly IProcessDiscoveryInsightService _processDiscoveryInsightService = processDiscoveryInsightService;
    private readonly IExecutiveInsightService _executiveInsightService = executiveInsightService;

    public async Task<ControlTowerContext> BuildContextAsync()
    {
        var adoptionFindings = await _adoptionIntelligenceService.GetFindingsAsync();
        var complianceInsights = await _complianceInsightService.GetInsightsAsync();
        var countryReports = await _countryGovernanceService.GetCountryReportsAsync();
        var processFindings = await _processDiscoveryInsightService.DiscoverInsightsAsync();
        var executiveInsights = await _executiveInsightService.GetInsightsAsync();

        var context = new ControlTowerContext
        {
            AdoptionFindings = adoptionFindings
                .Select(f => new SupportingInsight { Source = "Adoption Intelligence", Finding = f.Finding })
                .ToList(),

            ComplianceFindings =
            [
                new SupportingInsight
                {
                    Source = "Compliance Insights",
                    Finding = $"Average compliance score across all requests is {complianceInsights.AverageComplianceScore:F0}."
                },
                .. complianceInsights.AverageComplianceScorePerCategory.Select(kvp =>
                    new SupportingInsight
                    {
                        Source = "Compliance Insights",
                        Finding = $"{kvp.Key} has an average compliance score of {kvp.Value:F0}."
                    }),
                .. complianceInsights.AverageComplianceScorePerCountry.Select(kvp =>
                    new SupportingInsight
                    {
                        Source = "Compliance Insights",
                        Finding = $"{kvp.Key} has an average compliance score of {kvp.Value:F0}."
                    })
            ],

            GovernanceFindings = countryReports
                .Select(r => new SupportingInsight
                {
                    Source = "Country Governance",
                    Finding = $"{r.Country}: {r.TotalRequests} request(s), compliance score {r.ComplianceScore}, " +
                              $"request quality score {r.RequestQualityScore}, {r.PolicyDeviationCount} policy deviation(s), " +
                              $"top categories: {string.Join(", ", r.TopCategories)}."
                })
                .ToList(),

            ProcessDiscoveryFindings = processFindings
                .Select(f => new SupportingInsight { Source = "Process Discovery", Finding = $"[{f.Severity}] {f.Finding}" })
                .ToList(),

            ExecutiveFindings = executiveInsights.TopFindings
                .Select(f => new SupportingInsight { Source = "Executive Insights", Finding = f })
                .Concat(executiveInsights.TopRisks.Select(r => new SupportingInsight { Source = "Executive Insights (Risk)", Finding = r }))
                .Concat(executiveInsights.TopOpportunities.Select(o => new SupportingInsight { Source = "Executive Insights (Opportunity)", Finding = o }))
                .ToList(),

            ExecutiveRecommendedActions = executiveInsights.RecommendedActions
        };

        return context;
    }
}

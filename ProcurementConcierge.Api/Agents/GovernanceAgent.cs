using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Wraps <see cref="ICountryGovernanceService"/>, <see cref="IComplianceInsightService"/>,
/// and <see cref="IProcessDiscoveryInsightService"/> as a plannable agent: synthesizes
/// broader procurement risk signals (repeated policy deviations, high-risk categories,
/// country-specific governance concerns) into a single <see cref="Contracts.GovernanceAssessment"/>.
/// </summary>
public class GovernanceAgent(
    ICountryGovernanceService countryGovernanceService,
    IComplianceInsightService complianceInsightService,
    IProcessDiscoveryInsightService processDiscoveryInsightService) : IAgent
{
    private readonly ICountryGovernanceService _countryGovernanceService = countryGovernanceService;
    private readonly IComplianceInsightService _complianceInsightService = complianceInsightService;
    private readonly IProcessDiscoveryInsightService _processDiscoveryInsightService = processDiscoveryInsightService;

    public string Name => nameof(GovernanceAgent);

    public async Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var countryReports = await _countryGovernanceService.GetCountryReportsAsync();
        var complianceInsights = await _complianceInsightService.GetInsightsAsync();
        var processFindings = await _processDiscoveryInsightService.DiscoverInsightsAsync();

        var findings = new List<string>();
        var recommendations = new List<string>();

        foreach (var report in countryReports.Where(r => r.PolicyDeviationCount > 0))
        {
            findings.Add($"{report.Country} has {report.PolicyDeviationCount} policy deviation(s) across {report.TotalRequests} request(s).");
        }

        foreach (var (category, score) in complianceInsights.AverageComplianceScorePerCategory
            .Where(kvp => kvp.Value < complianceInsights.AverageComplianceScore))
        {
            findings.Add($"{category} has a below-average compliance score ({score:F0}).");
            recommendations.Add($"Review procurement guidance for {category}.");
        }

        foreach (var insight in processFindings.Where(f => string.Equals(f.Severity, "High", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(insight.Finding);
            if (!string.IsNullOrWhiteSpace(insight.Recommendation))
            {
                recommendations.Add(insight.Recommendation);
            }
        }

        var riskLevel = findings.Count switch
        {
            0 => "Low",
            <= 2 => "Medium",
            _ => "High"
        };

        var assessment = new Contracts.GovernanceAssessment
        {
            RiskLevel = riskLevel,
            Findings = findings,
            Recommendations = recommendations.Distinct().ToList()
        };
        context.SetMemory(MemoryKeys.GovernanceAssessment, assessment);

        return new AgentExecutionStepResult
        {
            Success = true,
            Summary = $"Governance risk level: {riskLevel} ({findings.Count} finding(s)).",
            Outputs = { [MemoryKeys.GovernanceAssessment] = assessment }
        };
    }
}

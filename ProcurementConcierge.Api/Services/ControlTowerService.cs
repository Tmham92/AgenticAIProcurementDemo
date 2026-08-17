using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Combines interaction history, process discovery findings, executive insights, and
/// ProcOps dependency metrics into a single Procurement Control Tower dashboard,
/// providing procurement leadership a unified view of adoption and governance
/// effectiveness.
/// </summary>
public class ControlTowerService(
    IInteractionLoggingService interactionLoggingService,
    IProcessDiscoveryService processDiscoveryService,
    IExecutiveInsightService executiveInsightService,
    IProcOpsDependencyService procOpsDependencyService,
    IAdoptionIntelligenceService adoptionIntelligenceService) : IControlTowerService
{
    private const int LowComplianceThreshold = 60;

    private readonly IInteractionLoggingService _interactionLoggingService = interactionLoggingService;
    private readonly IProcessDiscoveryService _processDiscoveryService = processDiscoveryService;
    private readonly IExecutiveInsightService _executiveInsightService = executiveInsightService;
    private readonly IProcOpsDependencyService _procOpsDependencyService = procOpsDependencyService;
    private readonly IAdoptionIntelligenceService _adoptionIntelligenceService = adoptionIntelligenceService;

    public async Task<ControlTowerDashboard> GetDashboardAsync()
    {
        var records = await _interactionLoggingService.GetAllAsync();
        var executiveInsights = await _executiveInsightService.GetInsightsAsync();
        var procOpsMetrics = await _procOpsDependencyService.GetMetricsAsync();
        var processDiscoveryFindings = _processDiscoveryService.DiscoverFindings();
        var adoptionFindings = await _adoptionIntelligenceService.GetFindingsAsync();

        var dashboard = new ControlTowerDashboard
        {
            TopInsights = executiveInsights.TopFindings,
            RecommendedActions = [],
            AdoptionFindings = adoptionFindings
        };

        if (records.Count == 0)
        {
            dashboard.TopRisks.Add("No procurement requests have been processed yet.");
            return dashboard;
        }

        var knownFieldsCount = records.Count(r => RequestQualityHeuristics.HasKnownCategory(r.Category));
        dashboard.AdoptionScore = (int)Math.Round(knownFieldsCount * 100.0 / records.Count);

        dashboard.ComplianceScore = (int)Math.Round(records.Average(r => r.ComplianceScore));

        var interventionRequiredCount = procOpsMetrics.InterventionRequiredCount;
        dashboard.ProcOpsDependencyScore = (int)Math.Round(interventionRequiredCount * 100.0 / procOpsMetrics.TotalRequests);

        dashboard.PolicyDeviationCount = records.Count(r => r.PolicyDeviation);

        // Top risks: low compliance hotspots and high ProcOps dependency.
        if (dashboard.ComplianceScore < LowComplianceThreshold)
        {
            dashboard.TopRisks.Add($"Average compliance score ({dashboard.ComplianceScore}) is below the acceptable threshold of {LowComplianceThreshold}.");
        }

        if (dashboard.ProcOpsDependencyScore >= 30)
        {
            dashboard.TopRisks.Add($"{dashboard.ProcOpsDependencyScore}% of requests are likely to require ProcOps intervention.");
        }

        if (dashboard.PolicyDeviationCount > 0)
        {
            var deviationRate = (int)Math.Round(dashboard.PolicyDeviationCount * 100.0 / records.Count);
            dashboard.TopRisks.Add($"Policy deviations occur in {deviationRate}% of all requests ({dashboard.PolicyDeviationCount} total).");
        }

        dashboard.TopRisks.AddRange(processDiscoveryFindings);

        // Recommended actions, derived from the risks identified above.
        if (dashboard.ComplianceScore < LowComplianceThreshold)
        {
            dashboard.RecommendedActions.Add("Prioritize compliance coaching or policy clarification for low-scoring categories/countries.");
        }

        if (dashboard.ProcOpsDependencyScore >= 30)
        {
            dashboard.RecommendedActions.Add("Invest in self-service guidance to reduce reliance on manual ProcOps intervention.");
        }

        if (dashboard.PolicyDeviationCount > 0)
        {
            dashboard.RecommendedActions.Add("Review the most frequent policy deviation reasons and consider expanding preferred supplier coverage.");
        }

        if (dashboard.RecommendedActions.Count == 0)
        {
            dashboard.RecommendedActions.Add("Procurement adoption and governance metrics are healthy; continue monitoring.");
        }

        return dashboard;
    }
}

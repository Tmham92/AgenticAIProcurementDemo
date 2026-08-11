using System.Text.Json.Serialization;
using ProcurementConcierge.Api.Configuration;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Combines adoption insights, process discovery findings, and compliance insights
/// (all deterministic, code-computed analytics) into a ranked list of top executive
/// findings, then uses the AI Reasoning Layer (<see cref="ILLMService"/>) to synthesize
/// those findings into a CPO-style executive briefing (summary, risks, opportunities,
/// recommended actions). Business analytics remain fully deterministic; only the
/// narrative synthesis is LLM-generated.
/// </summary>
public class ExecutiveInsightService(
    IAdoptionInsightService adoptionInsightService,
    IProcessDiscoveryService processDiscoveryService,
    IProcessDiscoveryInsightService processDiscoveryInsightService,
    IComplianceInsightService complianceInsightService,
    ILLMService llmService,
    ILogger<ExecutiveInsightService> logger) : IExecutiveInsightService
{
    private readonly IAdoptionInsightService _adoptionInsightService = adoptionInsightService;
    private readonly IProcessDiscoveryService _processDiscoveryService = processDiscoveryService;
    private readonly IProcessDiscoveryInsightService _processDiscoveryInsightService = processDiscoveryInsightService;
    private readonly IComplianceInsightService _complianceInsightService = complianceInsightService;
    private readonly ILLMService _llmService = llmService;
    private readonly ILogger<ExecutiveInsightService> _logger = logger;

    private const string SystemPrompt = """
        You are an AI assistant supporting a Chief Procurement Officer (CPO). Given a list of
        procurement adoption, compliance, and process discovery findings, synthesize them into
        a concise executive briefing: an executive summary paragraph, the top risks, the top
        opportunities, and recommended actions.
        """;

    public async Task<ExecutiveInsights> GetInsightsAsync()
    {
        var findings = new List<string>();

        // Process discovery findings (e.g. unmapped categories per country, low supplier adoption).
        findings.AddRange(_processDiscoveryService.DiscoverFindings());

        // Structured process discovery insights derived from the persisted SQLite interaction log.
        var interactionInsights = await _processDiscoveryInsightService.DiscoverInsightsAsync();
        findings.AddRange(interactionInsights.Select(i =>
            string.IsNullOrWhiteSpace(i.Recommendation) ? i.Finding : $"{i.Finding} ({i.Recommendation})"));

        var adoption = await _adoptionInsightService.GetInsightsAsync();
        var compliance = await _complianceInsightService.GetInsightsAsync();

        if (adoption.TotalRequests > 0)
        {
            var nonPreferredSupplierCount = adoption.MostCommonDeviations
                .GetValueOrDefault("Non-Preferred Supplier", 0);
            var preferredSupplierUsageRate = 1.0 - ((double)nonPreferredSupplierCount / adoption.TotalRequests);
            findings.Add($"Preferred supplier usage is at {preferredSupplierUsageRate:P0}.");

            if (adoption.RequestsPerCategory.Count > 0)
            {
                var topCategory = adoption.RequestsPerCategory.OrderByDescending(kv => kv.Value).First();
                findings.Add($"'{topCategory.Key}' is the most frequently requested procurement category ({topCategory.Value} requests).");
            }

            if (compliance.AverageComplianceScorePerCategory.Count > 0)
            {
                var bestCategory = compliance.AverageComplianceScorePerCategory.OrderByDescending(kv => kv.Value).First();
                findings.Add($"'{bestCategory.Key}' has the highest average compliance score ({bestCategory.Value:F0}).");
            }

            if (adoption.MostCommonDeviations.Count > 0)
            {
                var topDeviation = adoption.MostCommonDeviations.First();
                findings.Add($"The most common policy deviation is '{topDeviation.Key}' ({topDeviation.Value} occurrences).");
            }
        }
        else
        {
            findings.Add("No procurement requests have been processed yet.");
        }

        var insights = new ExecutiveInsights { TopFindings = findings };

        await EnrichWithExecutiveBriefingAsync(insights);

        return insights;
    }

    private async Task EnrichWithExecutiveBriefingAsync(ExecutiveInsights insights)
    {
        try
        {
            var userPrompt =
                $"""
                Findings:
                {string.Join("\n", insights.TopFindings.Select(f => $"- {f}"))}

                Return JSON with fields: executiveSummary (string), topRisks (string array),
                topOpportunities (string array), recommendedActions (string array).
                """;

            var briefing = await _llmService.GenerateStructuredResponseAsync<BriefingResult>(SystemPrompt, userPrompt, ModelType.Insights);

            insights.ExecutiveSummary = briefing.ExecutiveSummary ?? string.Empty;
            insights.TopRisks = briefing.TopRisks ?? new List<string>();
            insights.TopOpportunities = briefing.TopOpportunities ?? new List<string>();
            insights.RecommendedActions = briefing.RecommendedActions ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM executive briefing generation failed. Falling back to raw findings only.");
            insights.ExecutiveSummary = string.Join(" ", insights.TopFindings);
        }
    }

    private class BriefingResult
    {
        [JsonPropertyName("executiveSummary")]
        public string? ExecutiveSummary { get; set; }

        [JsonPropertyName("topRisks")]
        public List<string>? TopRisks { get; set; }

        [JsonPropertyName("topOpportunities")]
        public List<string>? TopOpportunities { get; set; }

        [JsonPropertyName("recommendedActions")]
        public List<string>? RecommendedActions { get; set; }
    }
}

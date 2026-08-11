using System.Text.Json.Serialization;
using ProcurementConcierge.Api.Configuration;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Coaches the user towards a more complete procurement request before policy evaluation
/// takes place, using the AI Reasoning Layer (<see cref="ILLMService"/>) to assess
/// clarity/readiness and suggest improvements, falling back to deterministic heuristics
/// when the LLM is unavailable.
/// </summary>
public class ProcurementCoachingService(ILLMService llmService, ILogger<ProcurementCoachingService> logger) : IProcurementCoachingService
{
    private const int PointsPerDimension = 100 / 3;

    private readonly ILLMService _llmService = llmService;
    private readonly ILogger<ProcurementCoachingService> _logger = logger;

    private const string SystemPrompt = """
        You are a procurement coach. Review a procurement request for completeness and clarity
        before it goes through policy and compliance evaluation. Identify missing information
        (e.g. country, budget, business justification), score clarity and procurement readiness
        from 0-100, provide actionable suggestions, and propose an improved example of the
        request that includes the missing details.
        """;

    public async Task<ProcurementCoachingAssessment> CoachAsync(ProcurementAnalysis analysis)
    {
        try
        {
            var userPrompt =
                $"""
                Procurement request: "{analysis.OriginalMessage}"
                Extracted category: {analysis.Category}
                Extracted country: {analysis.Country}
                Extracted estimated spend: {analysis.EstimatedSpend}
                Extracted business justification: {analysis.BusinessJustification ?? "none"}

                Return JSON with fields: requestQualityScore (0-100), clarityScore (0-100),
                procurementReadinessScore (0-100), missingInformation (string array),
                improvementSuggestions (string array), suggestedRequest (string, an improved
                version of the request incorporating the missing details).
                """;

            var result = await _llmService.GenerateStructuredResponseAsync<CoachingResult>(SystemPrompt, userPrompt, ModelType.Coach);

            return new ProcurementCoachingAssessment
            {
                RequestQualityScore = Math.Clamp(result.RequestQualityScore, 0, 100),
                ClarityScore = Math.Clamp(result.ClarityScore, 0, 100),
                ProcurementReadinessScore = Math.Clamp(result.ProcurementReadinessScore, 0, 100),
                MissingInformation = result.MissingInformation ?? new List<string>(),
                ImprovementSuggestions = result.ImprovementSuggestions ?? new List<string>(),
                SuggestedRequest = result.SuggestedRequest ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM coaching failed. Falling back to heuristic coaching assessment.");
        }

        return FallbackCoach(analysis);
    }

    private class CoachingResult
    {
        [JsonPropertyName("requestQualityScore")]
        public int RequestQualityScore { get; set; }

        [JsonPropertyName("clarityScore")]
        public int ClarityScore { get; set; }

        [JsonPropertyName("procurementReadinessScore")]
        public int ProcurementReadinessScore { get; set; }

        [JsonPropertyName("missingInformation")]
        public List<string>? MissingInformation { get; set; }

        [JsonPropertyName("improvementSuggestions")]
        public List<string>? ImprovementSuggestions { get; set; }

        [JsonPropertyName("suggestedRequest")]
        public string? SuggestedRequest { get; set; }
    }

    /// <summary>
    /// Deterministic heuristic coaching used when the local LLM is unavailable or
    /// misconfigured, so the PoC remains demonstrable offline.
    /// </summary>
    private static ProcurementCoachingAssessment FallbackCoach(ProcurementAnalysis analysis)
    {
        var message = analysis.OriginalMessage ?? string.Empty;

        var hasCountry = RequestQualityHeuristics.HasKnownCountry(analysis.Country);

        var hasSpend = analysis.EstimatedSpend > 0;

        var hasJustification = RequestQualityHeuristics.HasJustification(message);

        var missingInformation = new List<string>();
        var suggestions = new List<string>();
        var score = 0;

        if (hasCountry)
        {
            score += PointsPerDimension;
        }
        else
        {
            missingInformation.Add("Country");
            suggestions.Add("Specify the country the request applies to (e.g. 'in France').");
        }

        if (hasSpend)
        {
            score += PointsPerDimension;
        }
        else
        {
            missingInformation.Add("Budget");
            suggestions.Add("Include an estimated budget (e.g. 'Estimated budget is €80,000').");
        }

        if (hasJustification)
        {
            score += PointsPerDimension;
        }
        else
        {
            missingInformation.Add("Business justification");
            suggestions.Add("Explain the business purpose of the request (e.g. 'for a product launch campaign').");
        }

        var assessment = new ProcurementCoachingAssessment
        {
            RequestQualityScore = score,
            ClarityScore = score,
            ProcurementReadinessScore = score,
            MissingInformation = missingInformation,
            ImprovementSuggestions = suggestions
        };

        if (score < 100)
        {
            assessment.SuggestedRequest = BuildSuggestedRequest(analysis, hasCountry, hasSpend, hasJustification);
        }

        return assessment;
    }

    private static string BuildSuggestedRequest(
        ProcurementAnalysis analysis, bool hasCountry, bool hasSpend, bool hasJustification)
    {
        var category = RequestQualityHeuristics.HasKnownCategory(analysis.Category)
            ? analysis.Category
            : "marketing agency";

        var country = hasCountry ? analysis.Country : "France";
        var spend = hasSpend ? analysis.EstimatedSpend.ToString("C") : "€80,000";
        var justification = hasJustification
            ? string.Empty
            : " for a product launch campaign";

        return $"I need a {category} in {country}{justification}. Estimated budget is {spend}.";
    }
}

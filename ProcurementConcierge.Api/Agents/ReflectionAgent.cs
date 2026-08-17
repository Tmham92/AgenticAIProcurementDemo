using System.Text.Json.Serialization;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Reflects on a completed agent execution run and determines whether the user's goal was
/// actually achieved, what information is still missing, and an overall confidence score.
/// Runs after the planned agents have executed, evaluating the accumulated working memory
/// rather than being itself part of the plannable tool list.
/// </summary>
public interface IReflectionAgent
{
    Task<ReflectionResult> ReflectAsync(AgentRunContext context);
}

/// <summary>
/// Combines deterministic checks (were required fields extracted with sufficient
/// confidence?) with an LLM-generated narrative summary and recommended next action via
/// <see cref="ILLMService"/>, so the pass/fail decision remains auditable and
/// reproducible while the explanation reads naturally.
/// </summary>
public class ReflectionAgent(ILLMService llmService, IClarificationService clarificationService, ILogger<ReflectionAgent> logger) : IReflectionAgent
{
    private readonly ILLMService _llmService = llmService;
    private readonly IClarificationService _clarificationService = clarificationService;
    private readonly ILogger<ReflectionAgent> _logger = logger;

    private const int ClarificationConfidenceThreshold = 70;

    private const string SystemPrompt = """
        You are the reflection module of an agentic procurement adoption platform. Given a
        summary of what was determined so far and any missing information, evaluate whether
        the user's goal was achieved, whether human intervention is required, and produce a
        short narrative summary plus a recommended next action.
        """;

    public async Task<ReflectionResult> ReflectAsync(AgentRunContext context)
    {
        var deterministic = DeterministicReflect(context);
        deterministic.Clarification = BuildClarification(context, deterministic);
        deterministic.RequiresClarification = deterministic.Clarification.ClarificationRequired;
        deterministic.RequiresReplanning = EvaluateRequiresReplanning(context, deterministic);

        try
        {
            var userPrompt =
                $"""
                Goal achieved (deterministic check): {deterministic.GoalAchieved}
                Deterministic reason: {deterministic.Reason}
                Missing information: {(deterministic.MissingInformation.Count > 0 ? string.Join(", ", deterministic.MissingInformation) : "none")}
                Confidence: {deterministic.Confidence}

                Return JSON with fields: reflectionSummary (string, a short narrative summary),
                recommendedNextAction (string), humanInterventionRequired (boolean).
                """;

            var result = await _llmService.GenerateStructuredResponseAsync<ReflectionLlmResult>(SystemPrompt, userPrompt);

            deterministic.ReflectionSummary = string.IsNullOrWhiteSpace(result.ReflectionSummary)
                ? deterministic.Reason
                : result.ReflectionSummary;
            deterministic.RecommendedNextAction = string.IsNullOrWhiteSpace(result.RecommendedNextAction)
                ? (deterministic.GoalAchieved ? "Proceed with the procurement request." : "Request the missing information from the user.")
                : result.RecommendedNextAction;
            deterministic.HumanInterventionRequired = result.HumanInterventionRequired || !deterministic.GoalAchieved;

            return deterministic;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM reflection failed. Falling back to deterministic reflection summary.");
        }

        deterministic.ReflectionSummary = deterministic.Reason;
        deterministic.RecommendedNextAction = deterministic.GoalAchieved
            ? "Proceed with the procurement request."
            : "Request the missing information from the user.";
        deterministic.HumanInterventionRequired = !deterministic.GoalAchieved;

        return deterministic;
    }

    /// <summary>
    /// Determines whether Dynamic Replanning should generate and execute an additional
    /// follow-up plan: the goal was not achieved, a clarification is not already going to
    /// short-circuit the flow, and/or key compliance data is still absent from working
    /// memory (e.g. because the planner's initial plan didn't include the right agents).
    /// </summary>
    private static bool EvaluateRequiresReplanning(AgentRunContext context, ReflectionResult deterministic)
    {
        if (deterministic.RequiresClarification)
        {
            // Clarification is itself handled as a (single-iteration) replanning path by
            // the orchestrator/ReplanningService, so it is not double-counted here.
            return false;
        }

        if (!deterministic.GoalAchieved)
        {
            return true;
        }

        var hasComplianceScore = context.GetMemory<ComplianceScoreResult>(MemoryKeys.ComplianceScoreResult) is not null;
        var hasGuidance = context.GetMemory<UserGuidanceResponse>(MemoryKeys.UserGuidance) is not null;

        return !hasComplianceScore || !hasGuidance;
    }

    /// <summary>
    /// Evaluates whether reflection has enough confidence in category, country, and spend
    /// to proceed, or whether the user must be asked a clarifying follow-up question.
    /// </summary>
    private ClarificationRequest BuildClarification(AgentRunContext context, ReflectionResult deterministic)
    {
        var analysis = context.GetMemory<ProcurementAnalysis>(MemoryKeys.Analysis);

        var missingInformation = new List<string>(deterministic.MissingInformation);

        var categoryConfidence = analysis?.CategoryConfidence ?? 0;
        var countryConfidence = analysis?.CountryConfidence ?? 0;
        var spendConfidence = analysis?.SpendConfidence ?? 0;

        var lowCategoryConfidence = categoryConfidence < ClarificationConfidenceThreshold;
        var lowCountryConfidence = countryConfidence < ClarificationConfidenceThreshold;
        var lowSpendConfidence = spendConfidence < ClarificationConfidenceThreshold;

        if (lowCategoryConfidence && !missingInformation.Any(m => m.Contains("categor", StringComparison.OrdinalIgnoreCase)))
        {
            missingInformation.Add("Procurement category");
        }

        if (lowCountryConfidence && !missingInformation.Any(m => m.Contains("countr", StringComparison.OrdinalIgnoreCase)))
        {
            missingInformation.Add("Country");
        }

        if (lowSpendConfidence && !missingInformation.Any(m => m.Contains("spend", StringComparison.OrdinalIgnoreCase)))
        {
            missingInformation.Add("Estimated spend amount");
        }

        var clarificationRequired = lowCategoryConfidence || lowCountryConfidence || lowSpendConfidence || missingInformation.Count > 0;

        if (!clarificationRequired)
        {
            return new ClarificationRequest
            {
                ClarificationRequired = false,
                MissingInformation = [],
                FollowUpQuestion = string.Empty,
                ConfidenceScore = deterministic.Confidence
            };
        }

        return new ClarificationRequest
        {
            ClarificationRequired = true,
            MissingInformation = missingInformation,
            FollowUpQuestion = _clarificationService.GenerateFollowUpQuestion(missingInformation),
            ConfidenceScore = deterministic.Confidence
        };
    }

    private class ReflectionLlmResult
    {
        [JsonPropertyName("reflectionSummary")]
        public string? ReflectionSummary { get; set; }

        [JsonPropertyName("recommendedNextAction")]
        public string? RecommendedNextAction { get; set; }

        [JsonPropertyName("humanInterventionRequired")]
        public bool HumanInterventionRequired { get; set; }
    }

    /// <summary>
    /// Deterministic checks that remain the source of truth for goal-achievement/
    /// confidence, independent of the LLM.
    /// </summary>
    private static ReflectionResult DeterministicReflect(AgentRunContext context)
    {
        var analysis = context.GetMemory<ProcurementAnalysis>(MemoryKeys.Analysis);
        var missingInformation = new List<string>();

        if (analysis is null)
        {
            return new ReflectionResult
            {
                GoalAchieved = false,
                Reason = "No request analysis was performed, so the procurement category, country, and spend could not be determined.",
                MissingInformation = { "Category", "Country", "Estimated spend" },
                Confidence = 0
            };
        }

        if (string.IsNullOrWhiteSpace(analysis.Category) || analysis.Category == "Unknown")
        {
            missingInformation.Add("Procurement category");
        }

        if (string.IsNullOrWhiteSpace(analysis.Country) || analysis.Country == "Unknown")
        {
            missingInformation.Add("Country");
        }

        if (analysis.EstimatedSpend <= 0)
        {
            missingInformation.Add("Estimated spend amount");
        }

        var confidence = (analysis.CategoryConfidence + analysis.CountryConfidence + analysis.SpendConfidence) / 3.0;
        var goalAchieved = missingInformation.Count == 0 && confidence >= 70;

        var reason = goalAchieved
            ? "All required procurement details were extracted with sufficient confidence."
            : missingInformation.Count > 0
                ? $"Unable to identify: {string.Join(", ", missingInformation)}."
                : "Extracted details have low confidence and should be verified.";

        return new ReflectionResult
        {
            GoalAchieved = goalAchieved,
            Reason = reason,
            MissingInformation = missingInformation,
            Confidence = Math.Round(confidence, 1)
        };
    }
}

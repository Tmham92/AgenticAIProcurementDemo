using System.Text.Json.Serialization;
using ProcurementConcierge.Api.Configuration;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Acts as a Procurement Adoption Coach whose sole purpose is to increase Coupa adoption
/// by minimizing employee effort: it identifies the correct process, required approvals,
/// and compliance risk, then gives a single simple next action. It deliberately never
/// rewrites the request, generates RFP-like content, procurement specifications, or asks
/// for unnecessary information.
/// </summary>
public class AdoptionGuidanceService(ILLMService llmService, ILogger<AdoptionGuidanceService> logger) : IAdoptionGuidanceService
{
    private const int MaxStatusWords = 5;
    private const int MaxNextActionWords = 10;
    private const int MaxApprovalWords = 5;
    private const int MaxTipWords = 10;
    private const int MaxTotalWords = 30;

    private readonly ILLMService _llmService = llmService;
    private readonly ILogger<AdoptionGuidanceService> _logger = logger;

    private const string SystemPrompt = """
        You are a Procurement Adoption Coach.

        Your goal is to get employees through procurement with the least possible effort.

        Do NOT generate procurement documents.

        Do NOT rewrite the user's request.

        Do NOT generate business justifications.

        Do NOT generate supplier requirements.

        Do NOT generate project descriptions.

        Focus only on:

        1. Current status
        2. Required action
        3. Required approval
        4. Helpful tip

        Keep all text extremely short.

        Respond in JSON only.
        """;

    public async Task<UserGuidanceResponse> BuildGuidanceAsync(
        ProcurementAnalysis analysis,
        ProcurementPolicy? policy,
        CountryRule? countryRule,
        ComplianceEvaluation evaluation,
        int complianceScore)
    {
        var guidanceLevel = Classify(analysis, policy, evaluation, complianceScore);
        var friendlinessScore = ToFriendlinessScore(guidanceLevel);

        var guidance = await GenerateGuidanceAsync(analysis, policy, evaluation, guidanceLevel)
            ?? BuildFallbackGuidance(analysis, policy, evaluation, guidanceLevel);

        guidance.GuidanceLevel = guidanceLevel;
        guidance.AdoptionFriendlinessScore = friendlinessScore;
        EnforceWordLimits(guidance);

        return guidance;
    }

    /// <summary>
    /// Deterministically classifies the request. This is business logic, not LLM-generated,
    /// so the guidance level and friendliness score stay predictable and auditable.
    /// </summary>
    private static GuidanceLevel Classify(
        ProcurementAnalysis analysis,
        ProcurementPolicy? policy,
        ComplianceEvaluation evaluation,
        int complianceScore)
    {
        if (policy is null || analysis.NeedsHumanReview)
        {
            return GuidanceLevel.HighRisk;
        }

        if (complianceScore < 50)
        {
            return GuidanceLevel.HighRisk;
        }

        if (evaluation.IsPolicyDeviation)
        {
            return GuidanceLevel.ReviewRequired;
        }

        if (complianceScore >= 90)
        {
            return GuidanceLevel.Easy;
        }

        return GuidanceLevel.ApprovalRequired;
    }

    private static int ToFriendlinessScore(GuidanceLevel level) => level switch
    {
        GuidanceLevel.Easy => 100,
        GuidanceLevel.ApprovalRequired => 75,
        GuidanceLevel.ReviewRequired => 50,
        GuidanceLevel.HighRisk => 0,
        _ => 25
    };

    private async Task<UserGuidanceResponse?> GenerateGuidanceAsync(
        ProcurementAnalysis analysis,
        ProcurementPolicy? policy,
        ComplianceEvaluation evaluation,
        GuidanceLevel guidanceLevel)
    {
        try
        {
            var userPrompt =
                $"""
                Category: {analysis.Category}
                Country: {analysis.Country}
                Spend: {analysis.EstimatedSpend}
                Compliance Score: {evaluation.ComplianceRisk}
                Compliance Status: {evaluation.ComplianceStatus}
                Approval Requirement: {evaluation.RequiredApproval}
                Policy Deviation: {evaluation.IsPolicyDeviation} ({evaluation.PolicyDeviationReason})
                Preferred Suppliers: {(policy is not null && policy.PreferredSuppliers.Count > 0 ? string.Join(", ", policy.PreferredSuppliers) : "none")}
                GuidanceLevel: {guidanceLevel}

                Return a JSON object only, with exactly these string properties: status, nextAction, approval, tip.
                Status: max 5 words. NextAction: max 10 words. Approval: max 5 words. Tip: max 10 words.
                """;

            var result = await _llmService.GenerateStructuredResponseAsync<GuidanceLlmResult>(SystemPrompt, userPrompt, ModelType.Coach);

            if (string.IsNullOrWhiteSpace(result.Status) || string.IsNullOrWhiteSpace(result.NextAction))
            {
                return null;
            }

            return new UserGuidanceResponse
            {
                Status = result.Status.Trim(),
                NextAction = result.NextAction.Trim(),
                Approval = (result.Approval ?? string.Empty).Trim(),
                Tip = (result.Tip ?? string.Empty).Trim()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate Procurement Adoption Coach guidance via LLM; using fallback guidance.");
            return null;
        }
    }

    /// <summary>
    /// Deterministic, ultra-short guidance used when the LLM is unavailable or returns an
    /// unusable response, matching the tone of the canonical examples.
    /// </summary>
    private static UserGuidanceResponse BuildFallbackGuidance(
        ProcurementAnalysis analysis,
        ProcurementPolicy? policy,
        ComplianceEvaluation evaluation,
        GuidanceLevel guidanceLevel)
    {
        if (analysis.NeedsHumanReview || policy is null)
        {
            return new UserGuidanceResponse
            {
                Status = "More information needed",
                NextAction = "Describe service required",
                Approval = "Pending",
                Tip = "Include budget and country"
            };
        }

        return guidanceLevel switch
        {
            GuidanceLevel.HighRisk => new UserGuidanceResponse
            {
                Status = "Escalation required",
                NextAction = "Contact procurement operations",
                Approval = "Pending review",
                Tip = "Add more request details"
            },
            GuidanceLevel.ReviewRequired => new UserGuidanceResponse
            {
                Status = "Policy exception required",
                NextAction = "Submit exception request",
                Approval = evaluation.RequiredApproval,
                Tip = policy.PreferredSuppliers.Count > 0
                    ? "Preferred suppliers approve faster"
                    : "Use a preferred supplier"
            },
            GuidanceLevel.ApprovalRequired => new UserGuidanceResponse
            {
                Status = "Ready to proceed",
                NextAction = "Create Coupa requisition",
                Approval = evaluation.RequiredApproval,
                Tip = "Add delivery location"
            },
            _ => new UserGuidanceResponse
            {
                Status = "Ready to proceed",
                NextAction = "Create Coupa requisition",
                Approval = "Manager approval",
                Tip = "Add delivery location"
            }
        };
    }

    /// <summary>
    /// Guards against the LLM ignoring the word-limit instructions by deterministically
    /// truncating each field, keeping the response as short as the design mandates.
    /// </summary>
    private static void EnforceWordLimits(UserGuidanceResponse guidance)
    {
        guidance.Status = TruncateToWords(guidance.Status, MaxStatusWords);
        guidance.NextAction = TruncateToWords(guidance.NextAction, MaxNextActionWords);
        guidance.Approval = TruncateToWords(guidance.Approval, MaxApprovalWords);
        guidance.Tip = TruncateToWords(guidance.Tip, MaxTipWords);

        var totalWords = CountWords(guidance.Status) + CountWords(guidance.NextAction)
            + CountWords(guidance.Approval) + CountWords(guidance.Tip);

        if (totalWords <= MaxTotalWords)
        {
            return;
        }

        // As a last resort, drop the tip first (least essential field) to respect the
        // overall 30-word budget.
        guidance.Tip = string.Empty;
    }

    private static string TruncateToWords(string text, int maxWords)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= maxWords ? text.Trim() : string.Join(' ', words.Take(maxWords));
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    private class GuidanceLlmResult
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("nextAction")]
        public string NextAction { get; set; } = string.Empty;

        [JsonPropertyName("approval")]
        public string? Approval { get; set; }

        [JsonPropertyName("tip")]
        public string? Tip { get; set; }
    }
}

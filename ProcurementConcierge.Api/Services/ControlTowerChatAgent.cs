using Microsoft.EntityFrameworkCore;
using ProcurementConcierge.Api.Configuration;
using ProcurementConcierge.Api.Data;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// The Control Tower Chat Agent: interprets executive questions about procurement
/// performance, adoption, governance, and compliance; classifies the question; gathers
/// grounding data via <see cref="IControlTowerContextBuilder"/>; and uses the AI Reasoning
/// Layer to generate a concise, citation-backed executive answer - never inventing metrics
/// that are not present in the supplied organizational data.
/// </summary>
public class ControlTowerChatAgent(
    IControlTowerContextBuilder contextBuilder,
    ILLMService llmService,
    InteractionDbContext dbContext,
    ILogger<ControlTowerChatAgent> logger) : IControlTowerChatAgent
{
    private readonly IControlTowerContextBuilder _contextBuilder = contextBuilder;
    private readonly ILLMService _llmService = llmService;
    private readonly InteractionDbContext _dbContext = dbContext;
    private readonly ILogger<ControlTowerChatAgent> _logger = logger;

    private const string ClassificationSystemPrompt = """
        Classify the following procurement leadership question into exactly one category:
        Adoption, Compliance, Governance, ProcOps, Executive, Country, General.

        - Adoption: questions about adoption rates, rollout, or usage of the procurement process/tool.
        - Compliance: questions about compliance scores, policy deviations, or supplier compliance.
        - Governance: questions about risk, governance concerns, or repeated policy issues.
        - ProcOps: questions about Procurement Operations workload or dependency.
        - Executive: broad strategic questions (e.g. where to invest, what to prioritize).
        - Country: questions specifically about a country's procurement performance.
        - General: anything that doesn't clearly fit the above.

        Respond with ONLY a JSON object: { "questionType": string }
        """;

    private const string ExecutiveAnswerSystemPrompt = """
        You are a Chief Procurement Advisor.

        You answer executive questions using only the supplied organizational data.

        Do not invent metrics.

        Reference findings.

        Provide concise recommendations.

        Use leadership language.

        Respond with ONLY a JSON object:
        {
          "answer": string,
          "citedFindings": [string],
          "recommendedActions": [string],
          "confidenceScore": number
        }

        "citedFindings" must be exact copies of findings you relied on from the supplied data
        (so they can be matched back to their source). "confidenceScore" is 0-100, reflecting
        how well the supplied data supports the answer (lower if data is sparse or missing).
        """;

    public async Task<ControlTowerAnswer> AskAsync(string question)
    {
        var questionType = await ClassifyAsync(question);
        var context = await _contextBuilder.BuildContextAsync();

        var answer = await GenerateAnswerAsync(question, questionType, context);

        await PersistConversationAsync(question, answer.Answer);

        return answer;
    }

    public async Task<List<ControlTowerConversation>> GetHistoryAsync()
    {
        return await _dbContext.ControlTowerConversations
            .AsNoTracking()
            .OrderByDescending(c => c.Timestamp)
            .ToListAsync();
    }

    private async Task<ControlTowerQuestionType> ClassifyAsync(string question)
    {
        try
        {
            var result = await _llmService.GenerateStructuredResponseAsync<ClassificationResult>(
                ClassificationSystemPrompt, $"Question: \"{question}\"", ModelType.ControlTowerChat);

            if (Enum.TryParse<ControlTowerQuestionType>(result.QuestionType, ignoreCase: true, out var parsed))
            {
                return parsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Control Tower Chat question classification failed. Falling back to heuristic classification.");
        }

        return HeuristicClassify(question);
    }

    private static ControlTowerQuestionType HeuristicClassify(string question)
    {
        var lower = question.ToLowerInvariant();

        if (lower.Contains("procops") || lower.Contains("procurement operations") || lower.Contains("workload"))
        {
            return ControlTowerQuestionType.ProcOps;
        }

        if (lower.Contains("adopt") || lower.Contains("rollout") || lower.Contains("usage"))
        {
            return ControlTowerQuestionType.Adoption;
        }

        if (lower.Contains("compliance") || lower.Contains("deviation") || lower.Contains("policy"))
        {
            return ControlTowerQuestionType.Compliance;
        }

        if (lower.Contains("risk") || lower.Contains("governance"))
        {
            return ControlTowerQuestionType.Governance;
        }

        if (lower.Contains("invest") || lower.Contains("priorit") || lower.Contains("strategy"))
        {
            return ControlTowerQuestionType.Executive;
        }

        if (lower.Contains("country") || lower.Contains("germany") || lower.Contains("france"))
        {
            return ControlTowerQuestionType.Country;
        }

        return ControlTowerQuestionType.General;
    }

    private async Task<ControlTowerAnswer> GenerateAnswerAsync(string question, ControlTowerQuestionType questionType, ControlTowerContext context)
    {
        var allFindings = context.AllFindings;
        var dataPackage = BuildDataPackage(context);

        var userPrompt = $"""
            Executive question ({questionType}): "{question}"

            Available organizational data:
            {dataPackage}
            """;

        try
        {
            var result = await _llmService.GenerateStructuredResponseAsync<AnswerResult>(
                ExecutiveAnswerSystemPrompt, userPrompt, ModelType.ControlTowerChat);

            var supportingInsights = MatchCitedFindings(result.CitedFindings, allFindings);

            return new ControlTowerAnswer
            {
                Question = question,
                Answer = result.Answer,
                SupportingInsights = supportingInsights,
                RecommendedActions = result.RecommendedActions,
                ConfidenceScore = Math.Clamp(result.ConfidenceScore, 0, 100)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Control Tower Chat answer generation failed. Falling back to deterministic summary.");
            return FallbackAnswer(question, context);
        }
    }

    private static string BuildDataPackage(ControlTowerContext context)
    {
        var sections = new List<string>();

        void AddSection(string title, List<SupportingInsight> insights)
        {
            if (insights.Count == 0)
            {
                return;
            }

            sections.Add($"{title}:\n" + string.Join("\n", insights.Select(i => $"- {i.Finding}")));
        }

        AddSection("Adoption Intelligence", context.AdoptionFindings);
        AddSection("Compliance Insights", context.ComplianceFindings);
        AddSection("Country Governance", context.GovernanceFindings);
        AddSection("Process Discovery", context.ProcessDiscoveryFindings);
        AddSection("Executive Insights", context.ExecutiveFindings);

        if (context.ExecutiveRecommendedActions.Count > 0)
        {
            sections.Add("Existing Executive Recommended Actions:\n" +
                string.Join("\n", context.ExecutiveRecommendedActions.Select(a => $"- {a}")));
        }

        return sections.Count == 0
            ? "No procurement data is available yet."
            : string.Join("\n\n", sections);
    }

    private static List<SupportingInsight> MatchCitedFindings(List<string> citedFindings, List<SupportingInsight> allFindings)
    {
        if (citedFindings.Count == 0)
        {
            return [];
        }

        var matched = new List<SupportingInsight>();

        foreach (var cited in citedFindings)
        {
            var match = allFindings.FirstOrDefault(f =>
                f.Finding.Contains(cited, StringComparison.OrdinalIgnoreCase) ||
                cited.Contains(f.Finding, StringComparison.OrdinalIgnoreCase));

            if (match is not null && !matched.Contains(match))
            {
                matched.Add(match);
            }
        }

        return matched;
    }

    private static ControlTowerAnswer FallbackAnswer(string question, ControlTowerContext context)
    {
        var topFindings = context.AllFindings.Take(3).ToList();

        return new ControlTowerAnswer
        {
            Question = question,
            Answer = topFindings.Count > 0
                ? "Based on available data: " + string.Join(" ", topFindings.Select(f => f.Finding))
                : "No procurement data is available yet to answer this question.",
            SupportingInsights = topFindings,
            RecommendedActions = context.ExecutiveRecommendedActions.Take(2).ToList(),
            ConfidenceScore = topFindings.Count > 0 ? 40 : 0
        };
    }

    private async Task PersistConversationAsync(string question, string answer)
    {
        _dbContext.ControlTowerConversations.Add(new ControlTowerConversation
        {
            Question = question,
            Answer = answer
        });

        await _dbContext.SaveChangesAsync();
    }

    private class ClassificationResult
    {
        public string QuestionType { get; set; } = string.Empty;
    }

    private class AnswerResult
    {
        public string Answer { get; set; } = string.Empty;
        public List<string> CitedFindings { get; set; } = [];
        public List<string> RecommendedActions { get; set; } = [];
        public int ConfidenceScore { get; set; }
    }
}

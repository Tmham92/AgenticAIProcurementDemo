using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Wraps <see cref="IAdoptionGuidanceService"/> as a plannable agent: builds the final
/// ultra-short Coupa adoption guidance from everything gathered so far in working memory.
/// </summary>
public class RecommendationAgent(IAdoptionGuidanceService adoptionGuidanceService) : IAgent
{
    private readonly IAdoptionGuidanceService _adoptionGuidanceService = adoptionGuidanceService;

    public string Name => nameof(RecommendationAgent);

    public async Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var analysis = context.GetMemory<ProcurementAnalysis>(MemoryKeys.Analysis)
            ?? throw new InvalidOperationException($"{nameof(RecommendationAgent)} requires {MemoryKeys.Analysis} in working memory.");
        var policy = context.GetMemory<ProcurementPolicy>(MemoryKeys.Policy);
        var countryRule = context.GetMemory<CountryRule>(MemoryKeys.CountryRule);
        var evaluation = context.GetMemory<ComplianceEvaluation>(MemoryKeys.ComplianceEvaluation)
            ?? new ComplianceEvaluation();
        var scoreResult = context.GetMemory<ComplianceScoreResult>(MemoryKeys.ComplianceScoreResult)
            ?? new ComplianceScoreResult();
        var organizationalInsight = context.GetMemory<OrganizationalMemoryInsight>(MemoryKeys.OrganizationalMemoryInsight);
        var knowledgeDocuments = context.GetMemory<List<KnowledgeDocument>>(MemoryKeys.KnowledgeDocuments) ?? [];

        var guidance = await _adoptionGuidanceService.BuildGuidanceAsync(
            analysis, policy, countryRule, evaluation, scoreResult.Score);

        ApplyOrganizationalInsight(guidance, organizationalInsight);
        ApplyKnowledgeReferences(guidance, knowledgeDocuments);

        context.SetMemory(MemoryKeys.UserGuidance, guidance);
        context.SetMemory(MemoryKeys.Recommendation, guidance.Status);
        context.SetMemory(MemoryKeys.RecommendedNextAction, guidance.NextAction);

        return new AgentExecutionStepResult
        {
            Success = true,
            Summary = guidance.NextAction,
            Outputs =
            {
                [MemoryKeys.UserGuidance] = guidance,
                [MemoryKeys.Recommendation] = guidance.Status,
                [MemoryKeys.RecommendedNextAction] = guidance.NextAction
            }
        };
    }

    /// <summary>
    /// Incorporates organizational memory findings (historical requests with the same
    /// category and country) into the guidance tip as a short, concise addition
    /// (max 15 words), per the rules: frequent policy deviations, typically high
    /// compliance, or high ProcOps dependency.
    /// </summary>
    private static void ApplyOrganizationalInsight(UserGuidanceResponse guidance, OrganizationalMemoryInsight? insight)
    {
        if (insight is null || insight.SimilarRequests == 0)
        {
            return;
        }

        var deviationRate = (double)insight.PolicyDeviationCount / insight.SimilarRequests;
        var procOpsRate = (double)insight.ProcOpsDependencyCount / insight.SimilarRequests;

        string? organizationalTip = null;

        if (deviationRate >= 0.5)
        {
            organizationalTip = "Similar requests often require supplier exceptions.";
        }
        else if (procOpsRate >= 0.5)
        {
            organizationalTip = "Similar requests often require procurement support.";
        }
        else if (insight.AverageComplianceScore >= 80)
        {
            organizationalTip = "Similar requests are normally approved without issues.";
        }

        if (organizationalTip is null)
        {
            return;
        }

        guidance.Tip = string.IsNullOrWhiteSpace(guidance.Tip)
            ? organizationalTip
            : $"{guidance.Tip} {organizationalTip}";
    }

    /// <summary>
    /// Surfaces the most relevant retrieved procurement documentation (policy guidance,
    /// procedural guidance, document references) as concise references, without expanding
    /// the ultra-short Status/NextAction/Approval/Tip fields.
    /// </summary>
    private static void ApplyKnowledgeReferences(UserGuidanceResponse guidance, List<KnowledgeDocument> documents)
    {
        if (documents.Count == 0)
        {
            return;
        }

        guidance.KnowledgeReferences = documents
            .Take(3)
            .Select(d => $"{d.Category}: {d.Title} ({d.Source})")
            .ToList();
    }
}

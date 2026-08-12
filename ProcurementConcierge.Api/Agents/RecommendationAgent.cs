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

        var guidance = await _adoptionGuidanceService.BuildGuidanceAsync(
            analysis, policy, countryRule, evaluation, scoreResult.Score);

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
}

using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Wraps <see cref="IRecommendationService"/> as a plannable agent: builds the final
/// procurement guidance narrative and recommended next action from everything gathered
/// so far in working memory.
/// </summary>
public class RecommendationAgent(IRecommendationService recommendationService) : IAgent
{
    private readonly IRecommendationService _recommendationService = recommendationService;

    public string Name => nameof(RecommendationAgent);

    public Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var analysis = context.GetMemory<ProcurementAnalysis>(MemoryKeys.Analysis)
            ?? throw new InvalidOperationException($"{nameof(RecommendationAgent)} requires {MemoryKeys.Analysis} in working memory.");
        var policy = context.GetMemory<ProcurementPolicy>(MemoryKeys.Policy);
        var countryRule = context.GetMemory<CountryRule>(MemoryKeys.CountryRule);
        var evaluation = context.GetMemory<ComplianceEvaluation>(MemoryKeys.ComplianceEvaluation)
            ?? new ComplianceEvaluation();

        var (recommendation, recommendedNextAction) = _recommendationService.BuildGuidance(analysis, policy, countryRule, evaluation);
        context.SetMemory(MemoryKeys.Recommendation, recommendation);
        context.SetMemory(MemoryKeys.RecommendedNextAction, recommendedNextAction);

        return Task.FromResult(new AgentExecutionStepResult
        {
            Success = true,
            Summary = recommendedNextAction,
            Outputs =
            {
                [MemoryKeys.Recommendation] = recommendation,
                [MemoryKeys.RecommendedNextAction] = recommendedNextAction
            }
        });
    }
}

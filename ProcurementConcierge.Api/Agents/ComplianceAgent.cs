using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Wraps <see cref="IComplianceService"/> and <see cref="IComplianceScoringService"/> as a
/// plannable agent: evaluates preferred supplier availability, required approval, and
/// overall compliance risk/score, storing the results in working memory.
/// </summary>
public class ComplianceAgent(
    IComplianceService complianceService,
    IComplianceScoringService complianceScoringService) : IAgent
{
    private readonly IComplianceService _complianceService = complianceService;
    private readonly IComplianceScoringService _complianceScoringService = complianceScoringService;

    public string Name => nameof(ComplianceAgent);

    public Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var analysis = context.GetMemory<ProcurementAnalysis>(MemoryKeys.Analysis)
            ?? throw new InvalidOperationException($"{nameof(ComplianceAgent)} requires {MemoryKeys.Analysis} in working memory.");
        var policy = context.GetMemory<ProcurementPolicy>(MemoryKeys.Policy);
        var countryRule = context.GetMemory<CountryRule>(MemoryKeys.CountryRule);
        var deviations = context.GetMemory<List<PolicyDeviationDetail>>(MemoryKeys.PolicyDeviations) ?? [];

        var evaluation = _complianceService.Evaluate(analysis, policy, countryRule);
        context.SetMemory(MemoryKeys.ComplianceEvaluation, evaluation);

        var scoreResult = _complianceScoringService.CalculateScore(deviations);
        context.SetMemory(MemoryKeys.ComplianceScoreResult, scoreResult);

        return Task.FromResult(new AgentExecutionStepResult
        {
            Success = true,
            Summary = $"Compliance Score: {scoreResult.Score} ({scoreResult.Level}), Risk: {evaluation.ComplianceRisk}, Required Approval: {evaluation.RequiredApproval}.",
            Outputs =
            {
                [MemoryKeys.ComplianceEvaluation] = evaluation,
                [MemoryKeys.ComplianceScoreResult] = scoreResult
            }
        });
    }
}

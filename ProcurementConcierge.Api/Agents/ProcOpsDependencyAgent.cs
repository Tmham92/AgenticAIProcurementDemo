using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Wraps <see cref="IProcOpsDependencyService"/> as a plannable agent: assesses whether
/// this request could have required Procurement Operations (ProcOps) intervention.
/// </summary>
public class ProcOpsDependencyAgent(IProcOpsDependencyService procOpsDependencyService) : IAgent
{
    private readonly IProcOpsDependencyService _procOpsDependencyService = procOpsDependencyService;

    public string Name => nameof(ProcOpsDependencyAgent);

    public Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var analysis = context.GetMemory<ProcurementAnalysis>(MemoryKeys.Analysis)
            ?? throw new InvalidOperationException($"{nameof(ProcOpsDependencyAgent)} requires {MemoryKeys.Analysis} in working memory.");
        var policy = context.GetMemory<ProcurementPolicy>(MemoryKeys.Policy);
        var scoreResult = context.GetMemory<ComplianceScoreResult>(MemoryKeys.ComplianceScoreResult) ?? new ComplianceScoreResult();

        var assessment = _procOpsDependencyService.Assess(analysis, policy, scoreResult.Score);
        context.SetMemory(MemoryKeys.ProcOpsAssessment, assessment);

        return Task.FromResult(new AgentExecutionStepResult
        {
            Success = true,
            Summary = assessment.InterventionRequired
                ? $"ProcOps intervention likely required: {assessment.Reason}"
                : "No ProcOps intervention expected.",
            Outputs = { [MemoryKeys.ProcOpsAssessment] = assessment }
        });
    }
}

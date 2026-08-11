using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Wraps <see cref="IPolicyService"/> and <see cref="IPolicyDeviationService"/> as a
/// plannable agent: retrieves the global procurement policy for the extracted category
/// and detects policy deviations, storing both in working memory.
/// </summary>
public class PolicyAgent(IPolicyService policyService, IPolicyDeviationService policyDeviationService) : IAgent
{
    private readonly IPolicyService _policyService = policyService;
    private readonly IPolicyDeviationService _policyDeviationService = policyDeviationService;

    public string Name => nameof(PolicyAgent);

    public async Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var analysis = context.GetMemory<ProcurementAnalysis>(MemoryKeys.Analysis)
            ?? throw new InvalidOperationException($"{nameof(PolicyAgent)} requires {MemoryKeys.Analysis} in working memory.");

        var policy = await _policyService.GetPolicyAsync(analysis.Category);
        context.SetMemory(MemoryKeys.Policy, policy!);

        var deviations = _policyDeviationService.Evaluate(analysis, policy);
        context.SetMemory(MemoryKeys.PolicyDeviations, deviations);

        return new AgentExecutionStepResult
        {
            Success = true,
            Summary = $"{(policy is not null ? "Policy Found" : "No Policy Found")}; {deviations.Count} deviation(s) detected.",
            Outputs =
            {
                [MemoryKeys.Policy] = policy!,
                [MemoryKeys.PolicyDeviations] = deviations
            }
        };
    }
}

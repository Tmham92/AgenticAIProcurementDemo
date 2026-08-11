using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Wraps <see cref="IProcessDiscoveryInsightService"/> as a plannable agent: analyzes
/// recorded interaction history to surface recurring compliance/adoption patterns,
/// useful for goals like "how compliant is marketing spend in France?".
/// </summary>
public class ProcessDiscoveryAgent(IProcessDiscoveryInsightService processDiscoveryInsightService) : IAgent
{
    private readonly IProcessDiscoveryInsightService _processDiscoveryInsightService = processDiscoveryInsightService;

    public string Name => nameof(ProcessDiscoveryAgent);

    public async Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var findings = await _processDiscoveryInsightService.DiscoverInsightsAsync();
        context.SetMemory(MemoryKeys.ProcessDiscoveryFindings, findings);

        return new AgentExecutionStepResult
        {
            Success = true,
            Summary = $"{findings.Count} process discovery finding(s) identified.",
            Outputs = { [MemoryKeys.ProcessDiscoveryFindings] = findings }
        };
    }
}

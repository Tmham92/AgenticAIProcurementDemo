using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Wraps <see cref="IAdoptionIntelligenceService"/> as a plannable agent: analyzes
/// procurement behavior across recorded interaction history to identify adoption problems
/// (repeated policy deviations, frequent supplier exceptions, low-compliance
/// countries/categories, high ProcOps dependency, and common missing information), storing
/// the resulting severity-ranked findings in shared working memory for downstream agents
/// (e.g. <see cref="GovernanceAgent"/>) and the Control Tower to consume.
/// </summary>
public class AdoptionIntelligenceAgent(IAdoptionIntelligenceService adoptionIntelligenceService) : IAgent
{
    private readonly IAdoptionIntelligenceService _adoptionIntelligenceService = adoptionIntelligenceService;

    public string Name => nameof(AdoptionIntelligenceAgent);

    public async Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var findings = await _adoptionIntelligenceService.GetFindingsAsync();

        context.SetMemory(MemoryKeys.AdoptionFindings, findings);

        var summary = findings.Count == 0
            ? "No significant adoption problems were detected in the recorded interaction history."
            : $"Identified {findings.Count} adoption finding(s); most severe: {findings[0].Finding}";

        return new AgentExecutionStepResult
        {
            Success = true,
            Summary = summary,
            Outputs = { [MemoryKeys.AdoptionFindings] = findings }
        };
    }
}

using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Wraps <see cref="IOrganizationalMemoryService"/> as a plannable agent: retrieves
/// historical organizational insight for the extracted category and country, and stores
/// it in working memory so <see cref="RecommendationAgent"/> can incorporate organizational
/// behavior into its guidance.
/// </summary>
public class OrganizationalMemoryAgent(IOrganizationalMemoryService organizationalMemoryService) : IAgent
{
    private readonly IOrganizationalMemoryService _organizationalMemoryService = organizationalMemoryService;

    public string Name => nameof(OrganizationalMemoryAgent);

    public async Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var analysis = context.GetMemory<ProcurementAnalysis>(MemoryKeys.Analysis)
            ?? throw new InvalidOperationException($"{nameof(OrganizationalMemoryAgent)} requires {MemoryKeys.Analysis} in working memory.");

        var insight = await _organizationalMemoryService.GetRelevantInsightAsync(analysis.Category, analysis.Country);
        context.SetMemory(MemoryKeys.OrganizationalMemoryInsight, insight);

        return new AgentExecutionStepResult
        {
            Success = true,
            Summary = $"Analyzed {insight.SimilarRequests} historical record(s): {insight.Summary}",
            Outputs = { [MemoryKeys.OrganizationalMemoryInsight] = insight }
        };
    }
}

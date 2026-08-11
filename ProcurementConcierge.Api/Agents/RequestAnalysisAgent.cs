using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Wraps <see cref="IRequestAnalysisService"/> as a plannable agent: extracts category,
/// country, and estimated spend from the original natural language request and stores
/// the result in working memory for downstream agents.
/// </summary>
public class RequestAnalysisAgent(IRequestAnalysisService analysisService) : IAgent
{
    private readonly IRequestAnalysisService _analysisService = analysisService;

    public string Name => nameof(RequestAnalysisAgent);

    public async Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var message = context.GetMemory<string>(MemoryKeys.OriginalMessage) ?? context.Goal.UserRequest;
        var analysis = await _analysisService.AnalyzeAsync(message);
        context.SetMemory(MemoryKeys.Analysis, analysis);

        return new AgentExecutionStepResult
        {
            Success = true,
            Summary = $"Category: {analysis.Category} ({analysis.CategoryConfidence}%), Country: {analysis.Country} ({analysis.CountryConfidence}%), Spend: {analysis.EstimatedSpend:C} ({analysis.SpendConfidence}%).",
            Outputs = { [MemoryKeys.Analysis] = analysis }
        };
    }
}

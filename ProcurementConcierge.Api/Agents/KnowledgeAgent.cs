using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Wraps <see cref="IKnowledgeRetrievalService"/> as a plannable agent: retrieves
/// procurement documentation (policies, Coupa guides, country procedures, SOPs, training
/// material, FAQs) relevant to the current request's category/country/question, storing
/// the results in working memory for <see cref="RecommendationAgent"/> and the execution
/// trace to consume.
/// </summary>
public class KnowledgeAgent(IKnowledgeRetrievalService knowledgeRetrievalService) : IAgent
{
    private readonly IKnowledgeRetrievalService _knowledgeRetrievalService = knowledgeRetrievalService;

    public string Name => nameof(KnowledgeAgent);

    public async Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var analysis = context.GetMemory<ProcurementAnalysis>(MemoryKeys.Analysis);
        var question = context.GetMemory<string>(MemoryKeys.OriginalMessage) ?? context.Goal.UserRequest;

        var scoredDocuments = await _knowledgeRetrievalService.RetrieveScoredAsync(analysis?.Category, analysis?.Country, question);
        var documents = scoredDocuments.Select(s => s.Document).ToList();

        context.SetMemory(MemoryKeys.KnowledgeDocuments, documents);

        var summary = scoredDocuments.Count == 0
            ? "No relevant procurement documentation was found."
            : $"Retrieved {scoredDocuments.Count} relevant document(s): " +
              string.Join(", ", scoredDocuments.Select(s => $"{s.Document.Source} (relevance {s.RelevanceScore})")) + ".";

        return new AgentExecutionStepResult
        {
            Success = true,
            Summary = summary,
            Outputs =
            {
                [MemoryKeys.KnowledgeDocuments] = documents,
                ["KnowledgeRelevanceScores"] = scoredDocuments.ToDictionary(s => s.Document.Id.ToString(), s => s.RelevanceScore)
            }
        };
    }
}

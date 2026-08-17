using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Retrieves procurement documentation relevant to a category, country, and natural
/// language question, using simple keyword matching over <see cref="IKnowledgeStore"/>
/// (no vector/semantic search yet).
/// </summary>
public interface IKnowledgeRetrievalService
{
    Task<List<KnowledgeDocument>> RetrieveAsync(string? category, string? country, string question);

    /// <summary>
    /// Same retrieval as <see cref="RetrieveAsync"/>, but also returns each document's
    /// keyword-match relevance score, so callers (e.g. <see cref="Agents.KnowledgeAgent"/>)
    /// can surface it in the execution trace.
    /// </summary>
    Task<List<ScoredKnowledgeDocument>> RetrieveScoredAsync(string? category, string? country, string question);
}

/// <summary>
/// A <see cref="KnowledgeDocument"/> paired with its keyword-match relevance score.
/// </summary>
public record ScoredKnowledgeDocument(KnowledgeDocument Document, int RelevanceScore);

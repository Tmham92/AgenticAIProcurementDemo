using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Keyword-matching implementation of <see cref="IKnowledgeRetrievalService"/>: filters
/// <see cref="IKnowledgeStore"/> documents by category/country, then ranks matches by the
/// number of question keywords found in the document's title/content (a simple relevance
/// score). No vector/semantic search - intentionally deterministic and offline-friendly,
/// consistent with the rest of the Concierge's fallback-first design.
/// </summary>
public class KnowledgeRetrievalService(IKnowledgeStore knowledgeStore) : IKnowledgeRetrievalService
{
    private const int MaxResults = 5;

    private static readonly string[] StopWords =
    [
        "the", "a", "an", "is", "are", "do", "does", "for", "of", "to", "in", "on",
        "what", "how", "can", "i", "we", "my", "our", "and", "or", "with", "about"
    ];

    private readonly IKnowledgeStore _knowledgeStore = knowledgeStore;

    public async Task<List<KnowledgeDocument>> RetrieveAsync(string? category, string? country, string question)
    {
        var scored = await RetrieveScoredAsync(category, country, question);
        return scored.Select(s => s.Document).ToList();
    }

    public async Task<List<ScoredKnowledgeDocument>> RetrieveScoredAsync(string? category, string? country, string question)
    {
        var candidates = await _knowledgeStore.SearchDocumentsAsync(category, country, keywords: null);

        var keywords = ExtractKeywords(question);

        if (keywords.Length == 0)
        {
            return candidates.Take(MaxResults).Select(d => new ScoredKnowledgeDocument(d, 0)).ToList();
        }

        return candidates
            .Select(d => new ScoredKnowledgeDocument(d, ScoreDocument(d, keywords)))
            .Where(x => x.RelevanceScore > 0)
            .OrderByDescending(x => x.RelevanceScore)
            .Take(MaxResults)
            .ToList();
    }

    private static string[] ExtractKeywords(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return [];
        }

        return question
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(w => w.Trim('?', '.', ',', '!', ':', ';', '"', '\''))
            .Where(w => w.Length > 2 && !StopWords.Contains(w, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int ScoreDocument(KnowledgeDocument document, string[] keywords)
    {
        var score = 0;

        foreach (var keyword in keywords)
        {
            if (document.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                score += 2;
            }

            if (document.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                score += 1;
            }
        }

        return score;
    }
}

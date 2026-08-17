using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Persists and retrieves <see cref="KnowledgeDocument"/>s (procurement policies, Coupa user
/// guides, country procedures, SOPs, training material, FAQs). Backed initially by SQLite
/// via <see cref="Data.InteractionDbContext"/>.
/// </summary>
public interface IKnowledgeStore
{
    Task AddDocumentAsync(KnowledgeDocument document);

    /// <summary>
    /// Returns all documents whose <see cref="KnowledgeDocument.Title"/> or
    /// <see cref="KnowledgeDocument.Content"/> contains any of the given keywords
    /// (case-insensitive), optionally narrowed by category/country.
    /// </summary>
    Task<List<KnowledgeDocument>> SearchDocumentsAsync(string? category, string? country, string? keywords);

    Task<List<KnowledgeDocument>> GetDocumentsByCategoryAsync(string category);

    Task<List<KnowledgeDocument>> GetAllAsync();
}

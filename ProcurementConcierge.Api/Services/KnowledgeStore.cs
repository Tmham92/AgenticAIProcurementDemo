using Microsoft.EntityFrameworkCore;
using ProcurementConcierge.Api.Data;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// SQLite-backed implementation of <see cref="IKnowledgeStore"/>, persisting
/// <see cref="KnowledgeDocument"/>s via <see cref="InteractionDbContext"/>.
/// </summary>
public class KnowledgeStore(InteractionDbContext dbContext, ILogger<KnowledgeStore> logger) : IKnowledgeStore
{
    private readonly InteractionDbContext _dbContext = dbContext;
    private readonly ILogger<KnowledgeStore> _logger = logger;

    public async Task AddDocumentAsync(KnowledgeDocument document)
    {
        if (document.Id == Guid.Empty)
        {
            document.Id = Guid.NewGuid();
        }

        _dbContext.KnowledgeDocuments.Add(document);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Added knowledge document {DocumentId} ({Title}).", document.Id, document.Title);
    }

    public async Task<List<KnowledgeDocument>> SearchDocumentsAsync(string? category, string? country, string? keywords)
    {
        var query = _dbContext.KnowledgeDocuments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(d => d.Category == category || d.Category == "All");
        }

        if (!string.IsNullOrWhiteSpace(country) && !string.Equals(country, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(d => d.Country == country || d.Country == "All");
        }

        var documents = await query.ToListAsync();

        if (string.IsNullOrWhiteSpace(keywords))
        {
            return documents;
        }

        var terms = keywords
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 2)
            .ToArray();

        if (terms.Length == 0)
        {
            return documents;
        }

        return documents
            .Where(d => terms.Any(t =>
                d.Title.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                d.Content.Contains(t, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public async Task<List<KnowledgeDocument>> GetDocumentsByCategoryAsync(string category)
    {
        return await _dbContext.KnowledgeDocuments
            .AsNoTracking()
            .Where(d => d.Category == category || d.Category == "All")
            .ToListAsync();
    }

    public async Task<List<KnowledgeDocument>> GetAllAsync()
    {
        return await _dbContext.KnowledgeDocuments
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedOn)
            .ToListAsync();
    }
}

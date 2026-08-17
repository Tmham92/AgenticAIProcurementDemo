using Microsoft.AspNetCore.Mvc;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Controllers;

/// <summary>
/// Exposes procurement knowledge document management and retrieval: adding new
/// documentation (policies, Coupa user guides, country procedures, SOPs, training
/// material, FAQs) and searching existing documents by category/country/keywords.
/// </summary>
[ApiController]
[Route("api/knowledge")]
public class KnowledgeController(IKnowledgeStore knowledgeStore, IKnowledgeRetrievalService knowledgeRetrievalService) : ControllerBase
{
    private readonly IKnowledgeStore _knowledgeStore = knowledgeStore;
    private readonly IKnowledgeRetrievalService _knowledgeRetrievalService = knowledgeRetrievalService;

    /// <summary>
    /// Adds a new procurement knowledge document.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(KnowledgeDocument), StatusCodes.Status201Created)]
    public async Task<ActionResult<KnowledgeDocument>> AddDocument([FromBody] KnowledgeDocument document)
    {
        if (document.Id == Guid.Empty)
        {
            document.Id = Guid.NewGuid();
        }

        if (document.CreatedOn == default)
        {
            document.CreatedOn = DateTime.UtcNow;
        }

        await _knowledgeStore.AddDocumentAsync(document);

        return CreatedAtAction(nameof(AddDocument), new { id = document.Id }, document);
    }

    /// <summary>
    /// Searches procurement knowledge documents by category, country, and/or a natural
    /// language question, ranked by keyword relevance.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<KnowledgeDocument>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KnowledgeDocument>>> Search(
        [FromQuery] string? category,
        [FromQuery] string? country,
        [FromQuery] string? question)
    {
        var results = await _knowledgeRetrievalService.RetrieveAsync(category, country, question ?? string.Empty);
        return Ok(results);
    }
}

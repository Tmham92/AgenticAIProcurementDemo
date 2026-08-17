namespace ProcurementConcierge.Api.Models;

/// <summary>
/// A single piece of procurement documentation (e.g. Procurement Policy, Coupa User
/// Guide, Country Procedure, SOP, Training Material, FAQ) that agents can retrieve and
/// reason over via <see cref="Services.Interfaces.IKnowledgeRetrievalService"/>, moving the
/// Concierge beyond hardcoded <c>policies.json</c>/<c>countryRules.json</c> content.
/// </summary>
public class KnowledgeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The procurement category this document applies to, or "All" if category-agnostic.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The country this document applies to, or "All" if country-agnostic.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Where the document originated from (e.g. "Global Procurement Policy v3", "Coupa User Guide", "SOP-104").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}

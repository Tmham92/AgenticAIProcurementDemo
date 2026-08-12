namespace ProcurementConcierge.Api.Models;

/// <summary>
/// A persisted record of a single analyzed procurement request, captured for
/// long-term insight collection (e.g. reporting, analytics, ProcOps review) in the
/// local SQLite interaction log.
/// </summary>
public class InteractionRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string OriginalRequest { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal EstimatedSpend { get; set; }
    public int ComplianceScore { get; set; }
    public string ComplianceLevel { get; set; } = string.Empty;
    public bool PolicyDeviation { get; set; }
    public List<string> DeviationTypes { get; set; } = [];
    public string Recommendation { get; set; } = string.Empty;
}

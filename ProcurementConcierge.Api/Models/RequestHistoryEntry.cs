using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Models;

/// <summary>
/// A persisted record of a processed procurement request, used to power adoption
/// insights, process discovery, and executive reporting.
/// </summary>
public class RequestHistoryEntry
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string Category { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int ComplianceScore { get; set; }
    public string ComplianceLevel { get; set; } = string.Empty;
    public List<PolicyDeviationDetail> Deviations { get; set; } = [];
}

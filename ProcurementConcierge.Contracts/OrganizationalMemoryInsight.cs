namespace ProcurementConcierge.Contracts;

/// <summary>
/// Aggregated insight derived from historical <see cref="InteractionRecord"/>s that share
/// the same category and country as the current request, used to enrich guidance with
/// organizational behavior (Organizational Memory).
/// </summary>
public class OrganizationalMemoryInsight
{
    public string Category { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int SimilarRequests { get; set; }
    public double AverageComplianceScore { get; set; }
    public int PolicyDeviationCount { get; set; }
    public int ProcOpsDependencyCount { get; set; }
    public string Summary { get; set; } = string.Empty;
}

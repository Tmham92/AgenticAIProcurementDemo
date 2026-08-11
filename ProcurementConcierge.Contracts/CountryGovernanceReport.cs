namespace ProcurementConcierge.Contracts;

/// <summary>
/// Governance metrics for a single country, summarizing procurement behavior derived
/// from recorded interaction history.
/// </summary>
public class CountryGovernanceReport
{
    public string Country { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int ComplianceScore { get; set; }
    public int RequestQualityScore { get; set; }
    public int PolicyDeviationCount { get; set; }
    public List<string> TopCategories { get; set; } = new();
}

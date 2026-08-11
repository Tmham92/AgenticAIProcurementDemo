namespace ProcurementConcierge.Contracts;

/// <summary>
/// Aggregated adoption metrics computed from request history, used to power the
/// adoption dashboard.
/// </summary>
public class AdoptionInsights
{
    public int TotalRequests { get; set; }
    public Dictionary<string, int> RequestsPerCountry { get; set; } = new();
    public Dictionary<string, int> RequestsPerCategory { get; set; } = new();
    public Dictionary<string, int> MostCommonDeviations { get; set; } = new();
    public int UnknownCategoryCount { get; set; }
    public List<ComplianceTrendPoint> ComplianceTrend { get; set; } = new();
}

/// <summary>
/// A single point in the compliance score trend over time.
/// </summary>
public class ComplianceTrendPoint
{
    public DateTimeOffset Timestamp { get; set; }
    public int ComplianceScore { get; set; }
}

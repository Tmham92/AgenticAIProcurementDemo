namespace ProcurementConcierge.Contracts;

/// <summary>
/// Aggregated compliance metrics computed from request history, used to power the
/// compliance dashboard.
/// </summary>
public class ComplianceInsights
{
    public double AverageComplianceScore { get; set; }
    public Dictionary<string, int> ComplianceLevelDistribution { get; set; } = new();
    public Dictionary<string, double> AverageComplianceScorePerCategory { get; set; } = new();
    public Dictionary<string, double> AverageComplianceScorePerCountry { get; set; } = new();
}

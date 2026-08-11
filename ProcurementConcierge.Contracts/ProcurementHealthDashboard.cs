namespace ProcurementConcierge.Contracts;

/// <summary>
/// Blended 0-100 health scores summarizing procurement adoption, compliance, process
/// quality, and request quality, combined into a single overall health score for
/// procurement leadership.
/// </summary>
public class ProcurementHealthDashboard
{
    public int AdoptionScore { get; set; }
    public int ComplianceScore { get; set; }
    public int ProcessQualityScore { get; set; }
    public int RequestQualityScore { get; set; }
    public int OverallHealthScore { get; set; }
}

namespace ProcurementConcierge.Contracts;

/// <summary>
/// A single citation backing a Control Tower Chat answer, identifying which data source
/// (e.g. "Compliance Insights", "Country Governance") produced which finding.
/// </summary>
public class SupportingInsight
{
    public string Source { get; set; } = string.Empty;
    public string Finding { get; set; } = string.Empty;
}

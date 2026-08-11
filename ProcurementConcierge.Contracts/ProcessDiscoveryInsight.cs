namespace ProcurementConcierge.Contracts;

/// <summary>
/// A single structured insight surfaced by process discovery analysis over recorded
/// interaction history, e.g. "Marketing requests in France have a compliance score below 60."
/// </summary>
public class ProcessDiscoveryInsight
{
    public string Finding { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

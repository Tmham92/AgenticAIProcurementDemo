namespace ProcurementConcierge.Contracts;

/// <summary>
/// A single detected deviation from global procurement policy.
/// </summary>
public class PolicyDeviationDetail
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

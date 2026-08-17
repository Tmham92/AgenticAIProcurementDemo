namespace ProcurementConcierge.Contracts;

/// <summary>
/// The Control Tower Chat's executive-level response to a <see cref="ControlTowerQuestion"/>,
/// grounded in organizational data with explicit citations.
/// </summary>
public class ControlTowerAnswer
{
    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public List<SupportingInsight> SupportingInsights { get; set; } = [];

    public List<string> RecommendedActions { get; set; } = [];

    /// <summary>
    /// A 0-100 confidence score reflecting how much supporting organizational data grounded
    /// the answer (not a calibrated LLM probability).
    /// </summary>
    public int ConfidenceScore { get; set; }
}

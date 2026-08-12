namespace ProcurementConcierge.Contracts;

/// <summary>
/// Feedback on the quality of a submitted procurement request, coaching the user
/// towards requests that are easier to process, more compliant, and less likely to
/// require ProcOps intervention.
/// </summary>
public class RequestQualityAssessment
{
    public int RequestQualityScore { get; set; }
    public string QualityLevel { get; set; } = string.Empty;
    public List<string> MissingInformation { get; set; } = [];
    public List<string> ImprovementSuggestions { get; set; } = [];
}

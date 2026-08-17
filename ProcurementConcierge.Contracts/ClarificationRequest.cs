namespace ProcurementConcierge.Contracts;

/// <summary>
/// Represents a determination made after reflection that critical information is missing
/// or too uncertain to safely generate procurement guidance, along with a concise
/// follow-up question to ask the user.
/// </summary>
public class ClarificationRequest
{
    public bool ClarificationRequired { get; set; }
    public List<string> MissingInformation { get; set; } = [];
    public string FollowUpQuestion { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
}

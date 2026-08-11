namespace ProcurementConcierge.Api.Models;

/// <summary>
/// Numeric compliance score (0-100) for a procurement request and its corresponding
/// qualitative level (Low/Medium/High).
/// </summary>
public class ComplianceScoreResult
{
    public int Score { get; set; } = 100;
    public string Level { get; set; } = "High";
}

namespace ProcurementConcierge.Contracts;

/// <summary>
/// The Governance Agent's assessment of broader procurement risk, synthesized from
/// country governance data, compliance history, and process discovery findings.
/// </summary>
public class GovernanceAssessment
{
    public string RiskLevel { get; set; } = string.Empty;
    public List<string> Findings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

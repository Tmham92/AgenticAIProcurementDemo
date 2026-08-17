namespace ProcurementConcierge.Contracts;

/// <summary>
/// Procurement Control Tower dashboard: a single leadership-facing view combining
/// adoption, compliance, ProcOps dependency, policy deviations, top risks/insights, and
/// recommended actions across the procurement guidance system.
/// </summary>
public class ControlTowerDashboard
{
    public int AdoptionScore { get; set; }
    public int ComplianceScore { get; set; }
    public int ProcOpsDependencyScore { get; set; }
    public int PolicyDeviationCount { get; set; }

    public List<string> TopRisks { get; set; } = [];
    public List<string> TopInsights { get; set; } = [];
    public List<string> RecommendedActions { get; set; } = [];

    /// <summary>
    /// Structured, severity-ranked adoption problems identified by the Adoption
    /// Intelligence Agent, ordered by severity descending (most severe first).
    /// </summary>
    public List<AdoptionFinding> AdoptionFindings { get; set; } = [];
}

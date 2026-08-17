namespace ProcurementConcierge.Contracts;

/// <summary>
/// Aggregated organizational data package assembled by
/// ControlTowerContextBuilder for the Control Tower Chat Agent to reason over -
/// combining adoption, compliance, governance, process discovery, executive, and
/// organizational memory findings into a single set of grounded facts.
/// </summary>
public class ControlTowerContext
{
    public List<SupportingInsight> AdoptionFindings { get; set; } = [];

    public List<SupportingInsight> ComplianceFindings { get; set; } = [];

    public List<SupportingInsight> GovernanceFindings { get; set; } = [];

    public List<SupportingInsight> ProcessDiscoveryFindings { get; set; } = [];

    public List<SupportingInsight> ExecutiveFindings { get; set; } = [];

    public List<string> ExecutiveRecommendedActions { get; set; } = [];

    /// <summary>
    /// All findings flattened, in the order gathered, for convenient prompt-building and
    /// citation lookup.
    /// </summary>
    public List<SupportingInsight> AllFindings =>
        [.. AdoptionFindings, .. ComplianceFindings, .. GovernanceFindings, .. ProcessDiscoveryFindings, .. ExecutiveFindings];
}

namespace ProcurementConcierge.Contracts;

/// <summary>
/// Aggregated ProcOps dependency metrics computed over recorded interaction history.
/// </summary>
public class ProcOpsMetrics
{
    public int TotalRequests { get; set; }
    public int InterventionRequiredCount { get; set; }
    public int EstimatedProcOpsTicketsAvoided { get; set; }
    public Dictionary<string, int> InterventionReasons { get; set; } = new();
}

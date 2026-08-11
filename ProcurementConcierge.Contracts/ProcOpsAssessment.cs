namespace ProcurementConcierge.Contracts;

/// <summary>
/// Assessment of whether a procurement request could have required Procurement
/// Operations (ProcOps) intervention, e.g. due to an unknown category, missing policy,
/// low compliance score, or missing spend estimate.
/// </summary>
public class ProcOpsAssessment
{
    public bool InterventionRequired { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

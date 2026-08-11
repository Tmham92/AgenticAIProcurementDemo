namespace ProcurementConcierge.Api.Models;

/// <summary>
/// Result of evaluating a procurement request against global policy, country rules,
/// and preferred supplier availability.
/// </summary>
public class ComplianceEvaluation
{
    public bool PreferredSupplierAvailable { get; set; }
    public string RequiredApproval { get; set; } = string.Empty;
    public bool IsPolicyDeviation { get; set; }
    public string PolicyDeviationReason { get; set; } = string.Empty;
    public string ComplianceRisk { get; set; } = "Low";
    public string ComplianceStatus { get; set; } = string.Empty;
}

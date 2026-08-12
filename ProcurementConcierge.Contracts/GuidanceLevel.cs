namespace ProcurementConcierge.Contracts;

/// <summary>
/// Deterministic classification of how much friction stands between the employee and
/// submitting the request in Coupa, driving the guidance badge color in the UI.
/// </summary>
public enum GuidanceLevel
{
    /// <summary>Can proceed immediately with no approval or exception required.</summary>
    Easy,

    /// <summary>A standard approval (e.g. manager/director) is required before proceeding.</summary>
    ApprovalRequired,

    /// <summary>A policy exception or procurement review is required (e.g. non-preferred supplier).</summary>
    ReviewRequired,

    /// <summary>Compliance risk is too high or key details are missing; escalation is required.</summary>
    HighRisk
}

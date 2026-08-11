namespace ProcurementConcierge.Contracts;

/// <summary>
/// The level of human involvement required before a procurement request can proceed.
/// </summary>
public enum EscalationLevel
{
    Automatic,
    ReviewRequired,
    ApprovalRequired
}

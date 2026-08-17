using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Web.Components.Concierge;

/// <summary>
/// Shared UI formatting helpers (badge/border CSS classes, fallback text) used across the
/// Procurement Concierge guidance-result header and tab components, kept in one place to
/// avoid duplicating logic per component.
/// </summary>
public static class ConciergeUiHelpers
{
    public static string RiskBadgeClass(string risk) => risk switch
    {
        "High" => "bg-danger",
        "Medium" => "bg-warning text-dark",
        "Low" => "bg-success",
        _ => "bg-secondary"
    };

    public static string ComplianceBadgeClass(string compliance) => compliance switch
    {
        "High" => "bg-success",
        "Medium" => "bg-warning text-dark",
        "Low" => "bg-danger",
        _ => "bg-secondary"
    };

    public static string GuidanceLevelBadgeClass(GuidanceLevel level) => level switch
    {
        GuidanceLevel.Easy => "bg-success",
        GuidanceLevel.ApprovalRequired => "bg-warning text-dark",
        GuidanceLevel.ReviewRequired => "bg-warning text-dark",
        GuidanceLevel.HighRisk => "bg-danger",
        _ => "bg-secondary"
    };

    public static string FriendlinessBarClass(int score) => score switch
    {
        >= 90 => "bg-success",
        >= 70 => "bg-info",
        >= 40 => "bg-warning",
        _ => "bg-danger"
    };

    public static string FriendlinessEmoji(int score) => score switch
    {
        >= 90 => "🟢",
        >= 70 => "🟡",
        >= 40 => "🟠",
        _ => "🔴"
    };

    public static string ExampleBadgeClass(string expectedComplianceLevel) => expectedComplianceLevel switch
    {
        "High" => "bg-success",
        "Medium" => "bg-warning text-dark",
        "Low" => "bg-danger",
        _ => "bg-secondary"
    };

    public static string EscalationBadgeClass(string level) => level switch
    {
        "Approval" => "bg-danger",
        "Review" => "bg-warning text-dark",
        "Automatic" => "bg-success",
        _ => "bg-secondary"
    };

    public static string EscalationBorderClass(string level) => level switch
    {
        "Approval" => "border-danger",
        "Review" => "border-warning",
        "Automatic" => "border-success",
        _ => ""
    };

    public static string QualityRiskEquivalent(string qualityLevel) => qualityLevel switch
    {
        "High" => "Low",
        "Medium" => "Medium",
        "Low" => "High",
        _ => "Medium"
    };

    /// <summary>
    /// True when the guidance card has no meaningful content in any of its four fields,
    /// which happens when the request quality was too low for the guidance service to
    /// produce a specific status/next action/approval/tip.
    /// </summary>
    public static bool HasNoGuidanceContent(UserGuidanceResponse guidance) =>
        string.IsNullOrWhiteSpace(guidance.Status)
        && string.IsNullOrWhiteSpace(guidance.NextAction)
        && string.IsNullOrWhiteSpace(guidance.Approval)
        && string.IsNullOrWhiteSpace(guidance.Tip);

    public static string FallbackText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Not available" : value;
}

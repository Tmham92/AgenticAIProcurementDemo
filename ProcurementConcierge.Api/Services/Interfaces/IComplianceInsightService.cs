using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Generates compliance metrics (average score, level distribution, per-category and
/// per-country averages) from the request history store, to power the compliance dashboard.
/// </summary>
public interface IComplianceInsightService
{
    Task<ComplianceInsights> GetInsightsAsync();
}

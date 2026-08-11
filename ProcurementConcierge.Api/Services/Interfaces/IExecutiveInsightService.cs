using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Synthesizes adoption metrics, process discovery findings, and compliance data into a
/// ranked list of executive-level findings, simulating a future Procurement Control Tower.
/// </summary>
public interface IExecutiveInsightService
{
    Task<ExecutiveInsights> GetInsightsAsync();
}

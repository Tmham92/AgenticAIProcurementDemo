using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Determines whether a procurement request could have required Procurement Operations
/// (ProcOps) intervention, and computes aggregated ProcOps dependency KPIs from recorded
/// interaction history.
/// </summary>
public interface IProcOpsDependencyService
{
    /// <summary>
    /// Assesses a single request's likelihood of requiring ProcOps intervention.
    /// </summary>
    ProcOpsAssessment Assess(Models.ProcurementAnalysis analysis, Models.ProcurementPolicy? policy, int complianceScore);

    /// <summary>
    /// Computes aggregated ProcOps dependency metrics, including the estimated number of
    /// ProcOps tickets avoided, from recorded interaction history.
    /// </summary>
    Task<ProcOpsMetrics> GetMetricsAsync();
}

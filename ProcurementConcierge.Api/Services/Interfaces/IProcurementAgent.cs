using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Generic extension point for future specialized agents (e.g. ComplianceAgent,
/// SpendIntelligenceAgent, SupplierOnboardingAgent, ProcurementPerformanceAgent).
/// Each agent can inspect/enrich the analysis and response as part of the pipeline.
/// </summary>
public interface IProcurementAgent
{
    /// <summary>
    /// A short, human readable name used for execution step logging.
    /// </summary>
    string AgentName { get; }

    /// <summary>
    /// Executes the agent's logic, potentially enriching the response.
    /// </summary>
    Task ExecuteAsync(ProcurementAnalysis analysis, ProcurementPolicy? policy, ProcurementResponse response);
}

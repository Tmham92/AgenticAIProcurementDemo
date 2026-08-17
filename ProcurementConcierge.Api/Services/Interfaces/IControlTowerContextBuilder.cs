using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Aggregates organizational data from Adoption Intelligence, Compliance Insights,
/// Country Governance, Process Discovery, and Executive Insights into a single
/// <see cref="ControlTowerContext"/> for the Control Tower Chat Agent to reason over.
/// </summary>
public interface IControlTowerContextBuilder
{
    Task<ControlTowerContext> BuildContextAsync();
}

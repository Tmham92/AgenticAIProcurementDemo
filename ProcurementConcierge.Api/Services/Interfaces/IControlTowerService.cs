using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Combines adoption, compliance, ProcOps dependency, and policy deviation data from
/// interaction history, process discovery, executive insights, and ProcOps dependency
/// analysis into a single Procurement Control Tower dashboard for leadership.
/// </summary>
public interface IControlTowerService
{
    Task<ControlTowerDashboard> GetDashboardAsync();
}

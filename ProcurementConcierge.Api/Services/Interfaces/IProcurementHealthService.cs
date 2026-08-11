using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Calculates blended 0-100 procurement health scores from recorded interaction history.
/// </summary>
public interface IProcurementHealthService
{
    Task<ProcurementHealthDashboard> GetHealthDashboardAsync();
}

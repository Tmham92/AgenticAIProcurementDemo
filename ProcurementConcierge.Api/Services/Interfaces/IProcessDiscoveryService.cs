namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Detects patterns in request history indicating where users struggle to comply with
/// procurement policy (e.g. frequent unmapped categories per country, low preferred
/// supplier adoption).
/// </summary>
public interface IProcessDiscoveryService
{
    List<string> DiscoverFindings();
}

using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Analyzes recorded interaction history to surface structured process discovery
/// insights (common categories/countries, recurring policy deviations, low compliance
/// hotspots, and unknown category usage).
/// </summary>
public interface IProcessDiscoveryInsightService
{
    Task<List<ProcessDiscoveryInsight>> DiscoverInsightsAsync();
}

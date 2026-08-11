using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Analyzes request history to surface process discovery findings - patterns that
/// indicate where users struggle to follow procurement policy.
/// </summary>
public class ProcessDiscoveryService(IRequestHistoryStore historyStore) : IProcessDiscoveryService
{
    private const int UnknownCategoryThreshold = 10;
    private const double LowPreferredSupplierAdoptionThreshold = 0.6;

    private readonly IRequestHistoryStore _historyStore = historyStore;

    public List<string> DiscoverFindings()
    {
        var entries = _historyStore.GetAll();
        var findings = new List<string>();

        if (entries.Count == 0)
        {
            return findings;
        }

        // Frequent unmapped categories per country.
        var unknownCategoryByCountry = entries
            .Where(e => string.Equals(e.Category, "Unknown", StringComparison.OrdinalIgnoreCase))
            .GroupBy(e => e.Country);

        foreach (var group in unknownCategoryByCountry)
        {
            if (group.Count() > UnknownCategoryThreshold)
            {
                findings.Add(
                    $"Users in {group.Key} frequently submit requests that cannot be mapped to existing procurement policies.");
            }
        }

        // Low preferred supplier adoption.
        var nonPreferredSupplierCount = entries.Count(e =>
            e.Deviations.Any(d => d.Type == "Non-Preferred Supplier"));
        var adoptionRate = 1.0 - ((double)nonPreferredSupplierCount / entries.Count);

        if (adoptionRate < LowPreferredSupplierAdoptionThreshold)
        {
            findings.Add("Preferred supplier adoption is low.");
        }

        return findings;
    }
}

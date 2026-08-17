using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;
using InteractionRecord = ProcurementConcierge.Api.Models.InteractionRecord;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Analyzes procurement behavior across recorded interaction history to identify concrete
/// adoption problems, surfaced as severity-ranked <see cref="AdoptionFinding"/>s consumed
/// by the <see cref="Agents.AdoptionIntelligenceAgent"/>, the adoption-intelligence
/// endpoint, and the Control Tower dashboard.
/// </summary>
public class AdoptionIntelligenceService(
    IInteractionLoggingService interactionLoggingService,
    IProcOpsDependencyService procOpsDependencyService) : IAdoptionIntelligenceService
{
    private const int MinSampleSize = 3;
    private const int LowComplianceThreshold = 60;
    private const int RepeatedDeviationThreshold = 3;
    private const double HighProcOpsDependencyRate = 30.0;
    private const double HighMissingInfoRate = 20.0;

    private readonly IInteractionLoggingService _interactionLoggingService = interactionLoggingService;
    private readonly IProcOpsDependencyService _procOpsDependencyService = procOpsDependencyService;

    public async Task<List<AdoptionFinding>> GetFindingsAsync()
    {
        var records = await _interactionLoggingService.GetAllAsync();
        var findings = new List<AdoptionFinding>();

        if (records.Count == 0)
        {
            return findings;
        }

        AddRepeatedPolicyDeviationFindings(records, findings);
        AddSupplierExceptionFindings(records, findings);
        AddLowComplianceCountryFindings(records, findings);
        AddLowComplianceCategoryFindings(records, findings);
        await AddHighProcOpsDependencyFindingAsync(findings);
        AddMissingInformationFindings(records, findings);

        return [.. findings.OrderByDescending(f => f.Severity)];
    }

    private static void AddRepeatedPolicyDeviationFindings(List<InteractionRecord> records, List<AdoptionFinding> findings)
    {
        var deviationGroups = records
            .SelectMany(r => r.DeviationTypes.Select(t => (Type: t, r.Category, r.Country)))
            .GroupBy(x => x.Type)
            .Where(g => g.Count() >= RepeatedDeviationThreshold);

        foreach (var group in deviationGroups)
        {
            var count = group.Count();
            var topCategory = group.GroupBy(x => x.Category).OrderByDescending(g => g.Count()).First().Key;
            var topCountry = group.GroupBy(x => x.Country).OrderByDescending(g => g.Count()).First().Key;

            findings.Add(new AdoptionFinding
            {
                Category = topCategory,
                Country = topCountry,
                Finding = $"Policy deviation '{group.Key}' has occurred {count} times.",
                Impact = "Repeated deviations from the same policy indicate a systemic gap between policy and real-world procurement needs, increasing compliance risk.",
                Recommendation = $"Review whether the policy driving '{group.Key}' deviations needs updating, or whether targeted requester guidance would reduce recurrence.",
                Severity = Math.Min(10, 4 + count / RepeatedDeviationThreshold)
            });
        }
    }

    private static void AddSupplierExceptionFindings(List<InteractionRecord> records, List<AdoptionFinding> findings)
    {
        var supplierDeviations = records
            .SelectMany(r => r.DeviationTypes.Select(t => (Type: t, r.Category, r.Country)))
            .Where(x => x.Type.Contains("supplier", StringComparison.OrdinalIgnoreCase) ||
                        x.Type.Contains("vendor", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (supplierDeviations.Count < RepeatedDeviationThreshold)
        {
            return;
        }

        var topCategory = supplierDeviations.GroupBy(x => x.Category).OrderByDescending(g => g.Count()).First().Key;
        var topCountry = supplierDeviations.GroupBy(x => x.Country).OrderByDescending(g => g.Count()).First().Key;

        findings.Add(new AdoptionFinding
        {
            Category = topCategory,
            Country = topCountry,
            Finding = $"Supplier/vendor exceptions have been requested {supplierDeviations.Count} times.",
            Impact = "Frequent supplier exceptions suggest the preferred supplier list does not reflect actual sourcing needs, driving off-catalog risk.",
            Recommendation = "Review the preferred supplier list for the affected category/country and consider onboarding frequently-requested suppliers.",
            Severity = Math.Min(10, 5 + supplierDeviations.Count / RepeatedDeviationThreshold)
        });
    }

    private static void AddLowComplianceCountryFindings(List<InteractionRecord> records, List<AdoptionFinding> findings)
    {
        var byCountry = records
            .GroupBy(r => r.Country)
            .Where(g => g.Count() >= MinSampleSize && g.Average(r => r.ComplianceScore) < LowComplianceThreshold);

        foreach (var group in byCountry)
        {
            var average = group.Average(r => r.ComplianceScore);
            findings.Add(new AdoptionFinding
            {
                Category = "All",
                Country = group.Key,
                Finding = $"Country '{group.Key}' has a low average compliance score of {average:F0} across {group.Count()} requests.",
                Impact = "Low country-level compliance indicates weak adoption of procurement policy or unclear local guidance.",
                Recommendation = $"Provide targeted training or localized policy guidance for requesters in '{group.Key}'.",
                Severity = Math.Min(10, 4 + (LowComplianceThreshold - (int)average) / 10)
            });
        }
    }

    private static void AddLowComplianceCategoryFindings(List<InteractionRecord> records, List<AdoptionFinding> findings)
    {
        var byCategory = records
            .GroupBy(r => r.Category)
            .Where(g => g.Count() >= MinSampleSize && g.Average(r => r.ComplianceScore) < LowComplianceThreshold);

        foreach (var group in byCategory)
        {
            var average = group.Average(r => r.ComplianceScore);
            findings.Add(new AdoptionFinding
            {
                Category = group.Key,
                Country = "All",
                Finding = $"Category '{group.Key}' has a low average compliance score of {average:F0} across {group.Count()} requests.",
                Impact = "Low category-level compliance suggests the policy for this category is unclear, outdated, or not well communicated.",
                Recommendation = $"Review and simplify the policy for '{group.Key}', or add category-specific guidance to reduce deviations.",
                Severity = Math.Min(10, 4 + (LowComplianceThreshold - (int)average) / 10)
            });
        }
    }

    private async Task AddHighProcOpsDependencyFindingAsync(List<AdoptionFinding> findings)
    {
        var metrics = await _procOpsDependencyService.GetMetricsAsync();

        if (metrics.TotalRequests < MinSampleSize)
        {
            return;
        }

        var rate = metrics.InterventionRequiredCount * 100.0 / metrics.TotalRequests;

        if (rate < HighProcOpsDependencyRate)
        {
            return;
        }

        var topReason = metrics.InterventionReasons.Count > 0
            ? metrics.InterventionReasons.OrderByDescending(r => r.Value).First().Key
            : "unclear category/policy assignment";

        findings.Add(new AdoptionFinding
        {
            Category = "All",
            Country = "All",
            Finding = $"{rate:F0}% of requests required ProcOps intervention (top reason: {topReason}).",
            Impact = "High ProcOps dependency increases manual workload and slows down procurement turnaround, indicating the self-service flow is not resolving enough requests.",
            Recommendation = "Address the most common ProcOps intervention reason directly in the guidance flow (e.g. clearer category/policy prompts) to reduce manual escalations.",
            Severity = rate >= 50 ? 9 : 6
        });
    }

    private static void AddMissingInformationFindings(List<InteractionRecord> records, List<AdoptionFinding> findings)
    {
        var unknownCategoryCount = records.Count(r => !RequestQualityHeuristics.HasKnownCategory(r.Category));
        var unknownCountryCount = records.Count(r => !RequestQualityHeuristics.HasKnownCountry(r.Country));

        var unknownCategoryRate = unknownCategoryCount * 100.0 / records.Count;
        var unknownCountryRate = unknownCountryCount * 100.0 / records.Count;

        if (unknownCategoryRate >= HighMissingInfoRate)
        {
            findings.Add(new AdoptionFinding
            {
                Category = "Unknown",
                Country = "All",
                Finding = $"{unknownCategoryRate:F0}% of requests ({unknownCategoryCount}) did not specify a recognizable procurement category.",
                Impact = "Missing category information prevents automatic policy lookup, forcing manual triage and delaying resolution.",
                Recommendation = "Add category selection prompts or examples to the request intake flow to reduce ambiguous submissions.",
                Severity = unknownCategoryRate >= 40 ? 8 : 5
            });
        }

        if (unknownCountryRate >= HighMissingInfoRate)
        {
            findings.Add(new AdoptionFinding
            {
                Category = "All",
                Country = "Unknown",
                Finding = $"{unknownCountryRate:F0}% of requests ({unknownCountryCount}) did not specify a recognizable country.",
                Impact = "Missing country information prevents applying country-specific procurement rules, increasing the risk of non-compliant purchases.",
                Recommendation = "Prompt requesters to confirm the country of purchase/delivery during intake.",
                Severity = unknownCountryRate >= 40 ? 8 : 5
            });
        }
    }
}

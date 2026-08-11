using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Evaluates a procurement request against global policy and produces a list of
/// detected policy deviations (unknown category, non-preferred supplier, missing
/// spend amount, missing country, missing business justification).
/// </summary>
public class PolicyDeviationService : IPolicyDeviationService
{
    public List<PolicyDeviationDetail> Evaluate(ProcurementAnalysis analysis, ProcurementPolicy? policy)
    {
        var deviations = new List<PolicyDeviationDetail>();

        // Unknown category
        if (!RequestQualityHeuristics.HasKnownCategory(analysis.Category) || policy is null)
        {
            deviations.Add(new PolicyDeviationDetail
            {
                Type = "Unknown Category",
                Severity = "High",
                Description = "The procurement category could not be mapped to an existing procurement policy.",
                RecommendedAction = "Clarify the product or service being procured, or escalate to procurement operations to classify it."
            });
        }

        // Non-preferred supplier
        if (policy is not null && policy.PreferredSuppliers.Count > 0)
        {
            var mentionsPreferredSupplier = policy.PreferredSuppliers.Any(supplier =>
                analysis.OriginalMessage.Contains(supplier, StringComparison.OrdinalIgnoreCase));

            if (!mentionsPreferredSupplier)
            {
                deviations.Add(new PolicyDeviationDetail
                {
                    Type = "Non-Preferred Supplier",
                    Severity = "Medium",
                    Description = $"The request does not reference any of the preferred suppliers for '{policy.Category}' ({string.Join(", ", policy.PreferredSuppliers)}).",
                    RecommendedAction = "Confirm whether a preferred supplier can be used before sourcing a new supplier."
                });
            }
        }

        // Missing spend amount
        if (analysis.EstimatedSpend <= 0)
        {
            deviations.Add(new PolicyDeviationDetail
            {
                Type = "Missing Spend Amount",
                Severity = "Medium",
                Description = "No estimated spend amount could be determined from the request.",
                RecommendedAction = "Ask the requester to provide an estimated budget so the correct approval level can be determined."
            });
        }

        // Missing country
        if (!RequestQualityHeuristics.HasKnownCountry(analysis.Country))
        {
            deviations.Add(new PolicyDeviationDetail
            {
                Type = "Missing Country",
                Severity = "Medium",
                Description = "No country was identified, so country-specific guidance could not be applied.",
                RecommendedAction = "Ask the requester which country/entity the procurement applies to."
            });
        }

        // Missing business justification
        var hasJustification = RequestQualityHeuristics.HasJustification(analysis.OriginalMessage);

        if (!hasJustification)
        {
            deviations.Add(new PolicyDeviationDetail
            {
                Type = "Missing Business Justification",
                Severity = "Low",
                Description = "The request does not include a clear business justification for the purchase.",
                RecommendedAction = "Request a short business justification to support the approval decision."
            });
        }

        return deviations;
    }
}

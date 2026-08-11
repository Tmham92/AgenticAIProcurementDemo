using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Generates human-readable procurement guidance and recommended next action, emphasizing
/// process adoption and compliance rather than direct transaction creation.
/// </summary>
public class RecommendationService : IRecommendationService
{
    public (string Recommendation, string RecommendedNextAction) BuildGuidance(
        ProcurementAnalysis analysis,
        ProcurementPolicy? policy,
        CountryRule? countryRule,
        ComplianceEvaluation evaluation)
    {
        if (policy is null)
        {
            return (
                "No global procurement policy exists for this category. Escalate to the procurement operations team for manual policy review.",
                "Contact Procurement Operations to define a policy before sourcing a supplier."
            );
        }

        var recommendationParts = new List<string>();

        if (evaluation.PreferredSupplierAvailable)
        {
            recommendationParts.Add(
                $"Use an existing preferred supplier ({string.Join(", ", policy.PreferredSuppliers)}) for {analysis.Category} in {analysis.Country}.");
        }
        else
        {
            recommendationParts.Add(
                $"No preferred supplier is configured for {analysis.Category}; initiate a standard sourcing process.");
        }

        recommendationParts.Add($"Required approval: {evaluation.RequiredApproval}.");

        if (countryRule is not null && !string.IsNullOrWhiteSpace(countryRule.Guidance))
        {
            recommendationParts.Add(countryRule.Guidance);
        }

        if (evaluation.IsPolicyDeviation)
        {
            recommendationParts.Add($"Policy deviation noted: {evaluation.PolicyDeviationReason}");
        }

        var recommendation = string.Join(" ", recommendationParts);

        if (analysis.NeedsHumanReview)
        {
            var warnings = new List<string>();

            if (analysis.CategoryConfidence < 70)
            {
                warnings.Add("Category could not be confidently determined. Please clarify what product or service you wish to procure.");
            }

            if (analysis.CountryConfidence < 70)
            {
                warnings.Add("Country could not be confidently determined. Please clarify which country/entity this request applies to.");
            }

            if (analysis.SpendConfidence < 70)
            {
                warnings.Add("Estimated spend could not be confidently determined. Please provide a specific budget amount.");
            }

            recommendation = string.Join(" ", warnings) + " " + recommendation;
        }

        string nextAction;
        if (evaluation.ComplianceRisk == "High")
        {
            nextAction = "Escalate to procurement operations and legal for manual review before proceeding.";
        }
        else if (evaluation.ComplianceRisk == "Medium")
        {
            nextAction = evaluation.PreferredSupplierAvailable
                ? $"Obtain {evaluation.RequiredApproval} approval, then proceed with the preferred supplier while addressing the noted deviation."
                : $"Obtain {evaluation.RequiredApproval} approval and run a compliant sourcing process for a new supplier.";
        }
        else
        {
            nextAction = evaluation.PreferredSupplierAvailable
                ? $"Proceed directly with the preferred supplier after obtaining {evaluation.RequiredApproval} approval."
                : $"Proceed with standard supplier sourcing after obtaining {evaluation.RequiredApproval} approval.";
        }

        return (recommendation, nextAction);
    }
}

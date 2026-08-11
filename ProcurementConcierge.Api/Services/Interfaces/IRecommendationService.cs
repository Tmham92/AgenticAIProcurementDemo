using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Builds the final procurement guidance narrative and recommended next action based on
/// the analysis, policy, country rule, and compliance evaluation.
/// </summary>
public interface IRecommendationService
{
    (string Recommendation, string RecommendedNextAction) BuildGuidance(
        ProcurementAnalysis analysis,
        ProcurementPolicy? policy,
        CountryRule? countryRule,
        ComplianceEvaluation evaluation);
}

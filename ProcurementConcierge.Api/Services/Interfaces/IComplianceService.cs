using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Evaluates a procurement request against global policy and country-specific rules to
/// determine preferred supplier availability, required approval level, policy deviations,
/// and overall compliance risk.
/// </summary>
public interface IComplianceService
{
    ComplianceEvaluation Evaluate(ProcurementAnalysis analysis, ProcurementPolicy? policy, CountryRule? countryRule);
}

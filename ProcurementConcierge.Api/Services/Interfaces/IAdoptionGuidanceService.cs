using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Produces the ultra-short <see cref="UserGuidanceResponse"/> that guides the employee to
/// the correct Coupa workflow with the least possible effort: current status, required
/// action, required approval, and a helpful tip - never a procurement document, RFP
/// content, or a rewritten request.
/// </summary>
public interface IAdoptionGuidanceService
{
    Task<UserGuidanceResponse> BuildGuidanceAsync(
        ProcurementAnalysis analysis,
        ProcurementPolicy? policy,
        CountryRule? countryRule,
        ComplianceEvaluation evaluation,
        int complianceScore);
}

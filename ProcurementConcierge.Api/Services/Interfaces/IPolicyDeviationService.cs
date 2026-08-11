using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Detects deviations from global procurement policy for a given request, such as
/// unknown categories, non-preferred suppliers, missing spend/country, and missing
/// business justification.
/// </summary>
public interface IPolicyDeviationService
{
    List<PolicyDeviationDetail> Evaluate(ProcurementAnalysis analysis, ProcurementPolicy? policy);
}

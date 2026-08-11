using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Calculates a Request Quality Score (0-100) for a procurement request, based on
/// whether category, country, spend, business justification, and supplier information
/// are present, and coaches the user towards a higher quality request.
/// </summary>
public interface IRequestQualityService
{
    RequestQualityAssessment Assess(ProcurementAnalysis analysis, ProcurementPolicy? policy);
}

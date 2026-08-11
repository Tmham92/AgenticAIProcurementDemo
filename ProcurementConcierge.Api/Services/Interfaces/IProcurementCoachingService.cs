using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Evaluates a procurement request's completeness before policy evaluation, generating a
/// quality score, missing information, improvement suggestions, and a suggested improved
/// request.
/// </summary>
public interface IProcurementCoachingService
{
    Task<ProcurementCoachingAssessment> CoachAsync(Models.ProcurementAnalysis analysis);
}

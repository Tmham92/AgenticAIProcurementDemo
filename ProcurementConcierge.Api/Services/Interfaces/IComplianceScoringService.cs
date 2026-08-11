using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Computes a numeric compliance score (0-100) and qualitative level from detected
/// policy deviations, and derives the human-in-the-loop escalation requirement.
/// </summary>
public interface IComplianceScoringService
{
    ComplianceScoreResult CalculateScore(List<PolicyDeviationDetail> deviations);
    EscalationInfo DetermineEscalation(ComplianceScoreResult scoreResult);
}

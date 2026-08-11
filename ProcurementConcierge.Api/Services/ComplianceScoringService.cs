using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Computes a compliance score starting at 100 and deducting points for each detected
/// policy deviation type, then maps the score to a qualitative level and a
/// human-in-the-loop escalation requirement.
/// </summary>
public class ComplianceScoringService : IComplianceScoringService
{
    public ComplianceScoreResult CalculateScore(List<PolicyDeviationDetail> deviations)
    {
        var score = 100;

        foreach (var deviation in deviations)
        {
            score -= deviation.Type switch
            {
                "Unknown Category" => 50,
                "Missing Spend Amount" => 20,
                "Missing Country" => 10,
                "Non-Preferred Supplier" => 30,
                _ => 0
            };
        }

        score = Math.Clamp(score, 0, 100);

        var level = score switch
        {
            >= 80 => "High",
            >= 50 => "Medium",
            _ => "Low"
        };

        return new ComplianceScoreResult { Score = score, Level = level };
    }

    public EscalationInfo DetermineEscalation(ComplianceScoreResult scoreResult)
    {
        if (scoreResult.Score >= 90)
        {
            return new EscalationInfo
            {
                Level = "Automatic",
                Reason = "Compliance score is 90 or above; the request can proceed automatically."
            };
        }

        if (scoreResult.Score >= 70)
        {
            return new EscalationInfo
            {
                Level = "Review",
                Reason = "Compliance score is between 70 and 89; a procurement operations review is required before proceeding."
            };
        }

        return new EscalationInfo
        {
            Level = "Approval",
            Reason = "Compliance score is below 70; formal approval is required before proceeding."
        };
    }
}

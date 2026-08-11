using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Determines how much human involvement a procurement request requires (Automatic,
/// ReviewRequired, or ApprovalRequired) based on the compliance score plus additional
/// risk signals such as unknown category/country, multiple policy conflicts, and low
/// extraction confidence.
/// </summary>
public interface IEscalationAgent
{
    EscalationDecision Decide(AgentRunContext context);
}

public class EscalationAgent : IEscalationAgent
{
    public EscalationDecision Decide(AgentRunContext context)
    {
        var analysis = context.GetMemory<ProcurementAnalysis>(MemoryKeys.Analysis);
        var scoreResult = context.GetMemory<ComplianceScoreResult>(MemoryKeys.ComplianceScoreResult);
        var deviations = context.GetMemory<List<PolicyDeviationDetail>>(MemoryKeys.PolicyDeviations) ?? new();

        var score = scoreResult?.Score ?? 100;

        var level = score switch
        {
            >= 90 => EscalationLevel.Automatic,
            >= 70 => EscalationLevel.ReviewRequired,
            _ => EscalationLevel.ApprovalRequired
        };
        var reasons = new List<string>();

        if (score >= 90)
        {
            reasons.Add($"Compliance score {score} is high enough for automatic processing.");
        }
        else if (score >= 70)
        {
            reasons.Add($"Compliance score {score} requires a manual review.");
        }
        else
        {
            reasons.Add($"Compliance score {score} is below the review threshold and requires approval.");
        }

        if (analysis is null || string.IsNullOrWhiteSpace(analysis.Category) || analysis.Category == "Unknown")
        {
            level = Max(level, EscalationLevel.ReviewRequired);
            reasons.Add("Procurement category is unknown.");
        }

        if (analysis is null || string.IsNullOrWhiteSpace(analysis.Country) || analysis.Country == "Unknown")
        {
            level = Max(level, EscalationLevel.ReviewRequired);
            reasons.Add("Country is unknown.");
        }

        if (deviations.Count > 1)
        {
            level = Max(level, EscalationLevel.ApprovalRequired);
            reasons.Add($"{deviations.Count} policy conflicts detected.");
        }

        if (analysis is not null && analysis.NeedsHumanReview)
        {
            level = Max(level, EscalationLevel.ReviewRequired);
            reasons.Add("Extraction confidence is low.");
        }

        return new EscalationDecision
        {
            Level = level,
            Reason = string.Join(" ", reasons)
        };
    }

    private static EscalationLevel Max(EscalationLevel a, EscalationLevel b) => (EscalationLevel)Math.Max((int)a, (int)b);
}

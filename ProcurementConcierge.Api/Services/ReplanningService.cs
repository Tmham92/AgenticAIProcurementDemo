using ProcurementConcierge.Api.Agents;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Deterministically decides which additional agents (if any) must run in a follow-up
/// plan after reflection, based on the reflection outcome and the current state of
/// working memory. Enforces <see cref="MaxIterations"/> so the orchestrator's Dynamic
/// Replanning loop is always bounded.
/// </summary>
public class ReplanningService : IReplanningService
{
    public const int MaxIterations = 3;

    public ReplanDecision Decide(AgentRunContext context, ReflectionResult reflection)
    {
        var decision = new ReplanDecision
        {
            CurrentIteration = context.CurrentIteration,
            MaxIterations = MaxIterations
        };

        if (context.CurrentIteration >= MaxIterations)
        {
            decision.ReplanRequired = false;
            decision.Reason = $"Maximum iteration budget ({MaxIterations}) reached; stopping without further replanning.";
            return decision;
        }

        // Missing/uncertain category, country, or spend: the user must clarify before any
        // further agents can meaningfully run.
        if (reflection.RequiresClarification)
        {
            decision.ReplanRequired = true;
            decision.Reason = "Critical procurement information is missing or too uncertain to proceed.";
            decision.AdditionalTasks.Add(nameof(ClarificationAgent));
            return decision;
        }

        if (!reflection.RequiresReplanning)
        {
            decision.ReplanRequired = false;
            decision.Reason = "Goal achieved and all required data is present; no replanning required.";
            return decision;
        }

        // Supplier exception detected: needs a governance review before a final
        // recommendation can be produced.
        var deviations = context.GetMemory<List<PolicyDeviationDetail>>(MemoryKeys.PolicyDeviations) ?? [];
        var hasSupplierException = deviations.Any(d => d.Type.Contains("Supplier", StringComparison.OrdinalIgnoreCase));

        if (hasSupplierException && context.GetMemory<GovernanceAssessment>(MemoryKeys.GovernanceAssessment) is null)
        {
            decision.ReplanRequired = true;
            decision.Reason = "Supplier exception detected; governance review is required before generating final guidance.";
            decision.AdditionalTasks.Add(nameof(GovernanceAgent));
            decision.AdditionalTasks.Add(nameof(RecommendationAgent));
            return decision;
        }

        // Compliance score missing: a required input for escalation/guidance never ran.
        if (context.GetMemory<ComplianceScoreResult>(MemoryKeys.ComplianceScoreResult) is null)
        {
            decision.ReplanRequired = true;
            decision.Reason = "Compliance score is missing from working memory; compliance evaluation must run before completion.";
            decision.AdditionalTasks.Add(nameof(ComplianceAgent));
            return decision;
        }

        // Guidance missing: the final recommendation step never ran.
        if (context.GetMemory<UserGuidanceResponse>(MemoryKeys.UserGuidance) is null)
        {
            decision.ReplanRequired = true;
            decision.Reason = "Guidance has not yet been generated; the recommendation step must run before completion.";
            decision.AdditionalTasks.Add(nameof(RecommendationAgent));
            return decision;
        }

        decision.ReplanRequired = false;
        decision.Reason = "Goal not yet achieved, but no further actionable agents could be identified.";
        return decision;
    }
}

using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;
using InteractionRecord = ProcurementConcierge.Api.Models.InteractionRecord;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Detects whether a procurement request could have required Procurement Operations
/// (ProcOps) intervention (e.g. unknown category, missing policy, low compliance score,
/// missing spend estimate), and computes aggregated ProcOps dependency KPIs - including
/// the estimated number of ProcOps tickets avoided - from recorded interaction history.
/// </summary>
public class ProcOpsDependencyService(IInteractionLoggingService interactionLoggingService) : IProcOpsDependencyService
{
    private const int LowComplianceThreshold = 60;

    private readonly IInteractionLoggingService _interactionLoggingService = interactionLoggingService;

    public ProcOpsAssessment Assess(ProcurementAnalysis analysis, ProcurementPolicy? policy, int complianceScore)
    {
        var isUnknownCategory = !RequestQualityHeuristics.HasKnownCategory(analysis.Category);

        if (isUnknownCategory)
        {
            return new ProcOpsAssessment
            {
                InterventionRequired = true,
                Reason = "The procurement category could not be determined from the request.",
                Recommendation = "Ask the requester to clarify the category of goods or services needed, or route to ProcOps for manual categorization."
            };
        }

        if (policy is null)
        {
            return new ProcOpsAssessment
            {
                InterventionRequired = true,
                Reason = $"No procurement policy is defined for category '{analysis.Category}'.",
                Recommendation = "Route to ProcOps to define or confirm the applicable policy for this category before proceeding."
            };
        }

        if (complianceScore < LowComplianceThreshold)
        {
            return new ProcOpsAssessment
            {
                InterventionRequired = true,
                Reason = $"Compliance score of {complianceScore} is below the acceptable threshold of {LowComplianceThreshold}.",
                Recommendation = "Escalate to ProcOps for manual review of policy deviations before the request proceeds."
            };
        }

        if (analysis.EstimatedSpend <= 0)
        {
            return new ProcOpsAssessment
            {
                InterventionRequired = false,
                Reason = "No spend estimate was provided in the request.",
                Recommendation = "Ask the requester to provide an estimated budget; ProcOps involvement may still be avoidable if other details are complete."
            };
        }

        return new ProcOpsAssessment
        {
            InterventionRequired = false,
            Reason = "The request has a known category, an applicable policy, an acceptable compliance score, and a spend estimate.",
            Recommendation = "No ProcOps intervention is expected; proceed with the recommended next action."
        };
    }

    public async Task<ProcOpsMetrics> GetMetricsAsync()
    {
        var records = await _interactionLoggingService.GetAllAsync();

        var metrics = new ProcOpsMetrics
        {
            TotalRequests = records.Count
        };

        if (records.Count == 0)
        {
            return metrics;
        }

        var reasons = new Dictionary<string, int>();
        var interventionRequiredCount = 0;

        foreach (var record in records)
        {
            var (interventionRequired, reasonKey) = AssessRecord(record);
            if (interventionRequired)
            {
                interventionRequiredCount++;
                reasons[reasonKey] = reasons.GetValueOrDefault(reasonKey, 0) + 1;
            }
        }

        metrics.InterventionRequiredCount = interventionRequiredCount;
        metrics.EstimatedProcOpsTicketsAvoided = records.Count - interventionRequiredCount;
        metrics.InterventionReasons = reasons;

        return metrics;
    }

    private static (bool InterventionRequired, string ReasonKey) AssessRecord(InteractionRecord record)
    {
        var isUnknownCategory = !RequestQualityHeuristics.HasKnownCategory(record.Category);

        if (isUnknownCategory)
        {
            return (true, "Unknown Category");
        }

        if (record.ComplianceScore < LowComplianceThreshold)
        {
            return (true, "Low Compliance Score");
        }

        if (record.PolicyDeviation)
        {
            return (true, "Policy Deviation");
        }

        return (false, string.Empty);
    }
}

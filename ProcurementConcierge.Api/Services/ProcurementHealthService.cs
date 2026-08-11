using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;
using InteractionRecord = ProcurementConcierge.Api.Models.InteractionRecord;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Calculates blended 0-100 health scores (adoption, compliance, process quality, request
/// quality) from recorded interaction history, combined into a single overall Procurement
/// Health Score for leadership reporting.
/// </summary>
public class ProcurementHealthService(IInteractionLoggingService interactionLoggingService) : IProcurementHealthService
{
    private readonly IInteractionLoggingService _interactionLoggingService = interactionLoggingService;

    public async Task<ProcurementHealthDashboard> GetHealthDashboardAsync()
    {
        var records = await _interactionLoggingService.GetAllAsync();

        if (records.Count == 0)
        {
            return new ProcurementHealthDashboard();
        }

        var adoptionScore = CalculateAdoptionScore(records);
        var complianceScore = CalculateComplianceScore(records);
        var processQualityScore = CalculateProcessQualityScore(records);
        var requestQualityScore = CalculateRequestQualityScore(records);

        var overallHealthScore = (int)Math.Round(
            (adoptionScore + complianceScore + processQualityScore + requestQualityScore) / 4.0);

        return new ProcurementHealthDashboard
        {
            AdoptionScore = adoptionScore,
            ComplianceScore = complianceScore,
            ProcessQualityScore = processQualityScore,
            RequestQualityScore = requestQualityScore,
            OverallHealthScore = overallHealthScore
        };
    }

    private static int CalculateAdoptionScore(List<InteractionRecord> records)
    {
        var knownFieldsCount = records.Count(r =>
            RequestQualityHeuristics.HasKnownCategory(r.Category) && RequestQualityHeuristics.HasKnownCountry(r.Country));

        return (int)Math.Round(Percentage(knownFieldsCount, records.Count));
    }

    private static int CalculateComplianceScore(List<InteractionRecord> records)
    {
        return (int)Math.Round(records.Average(r => r.ComplianceScore));
    }

    private static int CalculateProcessQualityScore(List<InteractionRecord> records)
    {
        var deviationCount = records.Count(r => r.PolicyDeviation);
        var deviationPercentage = Percentage(deviationCount, records.Count);
        return (int)Math.Round(100 - deviationPercentage);
    }

    private static int CalculateRequestQualityScore(List<InteractionRecord> records)
    {
        var averageScore = records.Average(record =>
            RequestQualityHeuristics.CalculateScore(record.Category, record.Country, record.EstimatedSpend, record.OriginalRequest));

        return (int)Math.Round(averageScore);
    }

    private static double Percentage(int part, int total) => total == 0 ? 0 : part * 100.0 / total;
}

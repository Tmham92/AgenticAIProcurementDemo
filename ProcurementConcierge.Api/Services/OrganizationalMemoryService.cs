using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Organizational Memory: analyzes historical interaction records to find patterns related
/// to category, country, compliance, policy deviations, and ProcOps dependency, so current
/// guidance can be informed by past organizational procurement behavior.
/// </summary>
public class OrganizationalMemoryService(IInteractionLoggingService interactionLoggingService) : IOrganizationalMemoryService
{
    private const int ProcOpsDependencyComplianceThreshold = 60;

    private readonly IInteractionLoggingService _interactionLoggingService = interactionLoggingService;

    public async Task<OrganizationalMemoryInsight> GetRelevantInsightAsync(string category, string country)
    {
        var allRecords = await _interactionLoggingService.GetAllAsync();

        var similar = allRecords
            .Where(r => string.Equals(r.Category, category, StringComparison.OrdinalIgnoreCase)
                && string.Equals(r.Country, country, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (similar.Count == 0)
        {
            return new OrganizationalMemoryInsight
            {
                Category = category,
                Country = country,
                SimilarRequests = 0,
                AverageComplianceScore = 0,
                PolicyDeviationCount = 0,
                ProcOpsDependencyCount = 0,
                Summary = "No historical requests found for this category and country."
            };
        }

        var averageComplianceScore = similar.Average(r => r.ComplianceScore);
        var policyDeviationCount = similar.Count(r => r.PolicyDeviation);
        var procOpsDependencyCount = similar.Count(r => r.ComplianceScore < ProcOpsDependencyComplianceThreshold);

        return new OrganizationalMemoryInsight
        {
            Category = category,
            Country = country,
            SimilarRequests = similar.Count,
            AverageComplianceScore = Math.Round(averageComplianceScore, 1),
            PolicyDeviationCount = policyDeviationCount,
            ProcOpsDependencyCount = procOpsDependencyCount,
            Summary = BuildSummary(similar.Count, averageComplianceScore, policyDeviationCount, procOpsDependencyCount)
        };
    }

    private static string BuildSummary(int similarRequests, double averageComplianceScore, int policyDeviationCount, int procOpsDependencyCount)
    {
        var deviationRate = (double)policyDeviationCount / similarRequests;
        var procOpsRate = (double)procOpsDependencyCount / similarRequests;

        if (deviationRate >= 0.5)
        {
            return "Most similar requests required a policy deviation.";
        }

        if (procOpsRate >= 0.5)
        {
            return "Most similar requests required ProcOps intervention.";
        }

        if (averageComplianceScore >= 80)
        {
            return "Most similar requests were approved without issues.";
        }

        return $"{similarRequests} similar request(s) with average compliance score {Math.Round(averageComplianceScore, 1)}.";
    }
}

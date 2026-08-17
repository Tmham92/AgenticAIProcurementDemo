using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Analyzes historical <see cref="Models.InteractionRecord"/>s to find organizational
/// patterns (compliance, policy deviations, ProcOps dependency) for a given category and
/// country, so current recommendations can be informed by past procurement behavior.
/// </summary>
public interface IOrganizationalMemoryService
{
    Task<OrganizationalMemoryInsight> GetRelevantInsightAsync(string category, string country);
}

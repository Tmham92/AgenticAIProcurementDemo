using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Analyzes procurement behavior across recorded <see cref="Models.InteractionRecord"/>s
/// to identify concrete adoption problems - repeated policy deviations, frequent supplier
/// exceptions, countries/categories with low compliance, high ProcOps dependency, and
/// common missing information - surfaced as severity-ranked <see cref="AdoptionFinding"/>s.
/// </summary>
public interface IAdoptionIntelligenceService
{
    /// <summary>
    /// Returns adoption findings derived from interaction history, ordered by
    /// <see cref="AdoptionFinding.Severity"/> descending (most severe first).
    /// </summary>
    Task<List<AdoptionFinding>> GetFindingsAsync();
}

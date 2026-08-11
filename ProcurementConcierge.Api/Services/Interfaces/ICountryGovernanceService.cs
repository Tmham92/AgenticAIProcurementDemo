using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Analyzes recorded interaction history by country, producing per-country governance
/// metrics (total requests, average compliance/request quality scores, policy deviation
/// counts, and top categories).
/// </summary>
public interface ICountryGovernanceService
{
    Task<List<CountryGovernanceReport>> GetCountryReportsAsync();
}

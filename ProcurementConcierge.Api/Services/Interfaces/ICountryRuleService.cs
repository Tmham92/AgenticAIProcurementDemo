using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Responsible for loading and querying country-specific procurement guidance.
/// </summary>
public interface ICountryRuleService
{
    Task<CountryRule?> GetCountryRuleAsync(string country);
    Task<IReadOnlyList<CountryRule>> GetAllCountryRulesAsync();
}

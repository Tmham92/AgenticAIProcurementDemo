using System.Text.Json;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Loads and queries country-specific procurement guidance from Data/countryRules.json.
/// </summary>
public class CountryRuleService(ILogger<CountryRuleService> logger, IWebHostEnvironment environment) : ICountryRuleService
{
    private readonly ILogger<CountryRuleService> _logger = logger;
    private readonly string _countryRulesFilePath = Path.Combine(environment.ContentRootPath, "Data", "countryRules.json");
    private List<CountryRule>? _cachedRules;
    private static readonly SemaphoreSlim _loadLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<CountryRule>> GetAllCountryRulesAsync()
    {
        if (_cachedRules is not null)
        {
            return _cachedRules;
        }

        await _loadLock.WaitAsync();
        try
        {
            if (_cachedRules is null)
            {
                _logger.LogInformation("Loading country rules from {Path}", _countryRulesFilePath);

                if (!File.Exists(_countryRulesFilePath))
                {
                    _logger.LogWarning("Country rules file not found at {Path}. Returning empty list.", _countryRulesFilePath);
                    _cachedRules = [];
                }
                else
                {
                    var json = await File.ReadAllTextAsync(_countryRulesFilePath);
                    _cachedRules = JsonSerializer.Deserialize<List<CountryRule>>(json, JsonOptions)
                                   ?? [];
                }
            }
        }
        finally
        {
            _loadLock.Release();
        }

        return _cachedRules;
    }

    public async Task<CountryRule?> GetCountryRuleAsync(string country)
    {
        var rules = await GetAllCountryRulesAsync();
        return rules.FirstOrDefault(r =>
            string.Equals(r.Country, country, StringComparison.OrdinalIgnoreCase))
            ?? rules.FirstOrDefault(r => string.Equals(r.Country, "Unknown", StringComparison.OrdinalIgnoreCase));
    }
}

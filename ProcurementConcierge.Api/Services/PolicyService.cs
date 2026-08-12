using System.Text.Json;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

public class PolicyService(ILogger<PolicyService> logger, IWebHostEnvironment environment) : IPolicyService
{
    private readonly ILogger<PolicyService> _logger = logger;
    private readonly string _policiesFilePath = Path.Combine(environment.ContentRootPath, "Data", "policies.json");
    private List<ProcurementPolicy>? _cachedPolicies;
    private static readonly SemaphoreSlim _loadLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<ProcurementPolicy>> GetAllPoliciesAsync()
    {
        if (_cachedPolicies is not null)
        {
            return _cachedPolicies;
        }

        await _loadLock.WaitAsync();
        try
        {
            if (_cachedPolicies is null)
            {
                _logger.LogInformation("Loading procurement policies from {Path}", _policiesFilePath);

                if (!File.Exists(_policiesFilePath))
                {
                    _logger.LogWarning("Policies file not found at {Path}. Returning empty policy list.", _policiesFilePath);
                    _cachedPolicies = [];
                }
                else
                {
                    var json = await File.ReadAllTextAsync(_policiesFilePath);
                    _cachedPolicies = JsonSerializer.Deserialize<List<ProcurementPolicy>>(json, JsonOptions)
                                       ?? [];
                }
            }
        }
        finally
        {
            _loadLock.Release();
        }

        return _cachedPolicies;
    }

    public async Task<ProcurementPolicy?> GetPolicyAsync(string category)
    {
        var policies = await GetAllPoliciesAsync();
        return policies.FirstOrDefault(p =>
            string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase));
    }
}

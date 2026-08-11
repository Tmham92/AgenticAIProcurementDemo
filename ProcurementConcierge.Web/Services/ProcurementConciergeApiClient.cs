using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Web.Services;

/// <summary>
/// Calls the Procurement Concierge API's /api/concierge endpoint over HTTP, so the Blazor
/// frontend remains a thin presentation layer with no direct dependency on API-internal services.
/// </summary>
public class ProcurementConciergeApiClient(HttpClient httpClient, ILogger<ProcurementConciergeApiClient> logger) : IProcurementConciergeApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<ProcurementConciergeApiClient> _logger = logger;

    public async Task<ProcurementResponse> ProcessRequestAsync(ProcurementRequest request)
    {
        var httpResponse = await _httpClient.PostAsJsonAsync("api/concierge", request);
        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content.ReadFromJsonAsync<ProcurementResponse>();
        if (response is null)
        {
            _logger.LogError("Procurement Concierge API returned an empty response.");
            throw new InvalidOperationException("Procurement Concierge API returned an empty response.");
        }

        return response;
    }

    public async Task<AdoptionInsights> GetAdoptionInsightsAsync()
        => await GetAsync<AdoptionInsights>("api/dashboard/adoption");

    public async Task<ComplianceInsights> GetComplianceInsightsAsync()
        => await GetAsync<ComplianceInsights>("api/dashboard/compliance");

    public async Task<ExecutiveInsights> GetExecutiveInsightsAsync()
        => await GetAsync<ExecutiveInsights>("api/dashboard/executive-insights");

    public async Task<List<InteractionRecord>> GetInteractionsAsync()
        => await GetAsync<List<InteractionRecord>>("api/interactions") ?? new List<InteractionRecord>();

    public async Task<List<ProcessDiscoveryInsight>> GetProcessDiscoveryInsightsAsync()
        => await GetAsync<List<ProcessDiscoveryInsight>>("api/process-discovery") ?? new List<ProcessDiscoveryInsight>();

    private async Task<T> GetAsync<T>(string requestUri) where T : new()
    {
        var httpResponse = await _httpClient.GetAsync(requestUri);
        httpResponse.EnsureSuccessStatusCode();

        var result = await httpResponse.Content.ReadFromJsonAsync<T>();
        if (result is null)
        {
            _logger.LogError("Procurement Concierge API returned an empty response for {RequestUri}.", requestUri);
            return new T();
        }

        return result;
    }
}

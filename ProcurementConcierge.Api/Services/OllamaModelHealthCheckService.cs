using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProcurementConcierge.Api.Configuration;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Checks connectivity and model availability of the local Ollama runtime by querying
/// its "/api/tags" endpoint (lists locally pulled models) and measuring round-trip
/// latency, without invoking any business prompts.
/// </summary>
public class OllamaModelHealthCheckService(
    HttpClient httpClient,
    IOptions<OllamaSettings> options,
    ILogger<OllamaModelHealthCheckService> logger) : IModelHealthCheckService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly OllamaSettings _settings = options.Value;
    private readonly ILogger<OllamaModelHealthCheckService> _logger = logger;

    public async Task<OllamaStatus> CheckStatusAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await _httpClient.GetAsync("/api/tags");
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return new OllamaStatus
                {
                    Connected = false,
                    Model = _settings.Model,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    Error = $"Ollama returned status code {(int)response.StatusCode}."
                };
            }

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);

            var modelLoaded = false;
            if (document.RootElement.TryGetProperty("models", out var modelsElement) &&
                modelsElement.ValueKind == JsonValueKind.Array)
            {
                modelLoaded = modelsElement.EnumerateArray().Any(model =>
                    model.TryGetProperty("name", out var nameElement) &&
                    (nameElement.GetString()?.StartsWith(_settings.Model, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return new OllamaStatus
            {
                Connected = true,
                Model = _settings.Model,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                Error = modelLoaded ? null : $"Model '{_settings.Model}' was not found among locally pulled Ollama models."
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Ollama health check failed for {BaseUrl}.", _settings.BaseUrl);

            return new OllamaStatus
            {
                Connected = false,
                Model = _settings.Model,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }
}

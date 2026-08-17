using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProcurementConcierge.Api.Configuration;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// <see cref="ILLMService"/> implementation backed by a locally running Ollama instance,
/// communicating over Ollama's REST API. This is the AI Reasoning Layer's default
/// provider, running fully locally with no Azure/OpenAI dependency; it can be swapped for
/// <see cref="AzureOpenAIService"/> purely via DI. The model used per-call is resolved via
/// <see cref="IModelSelectionService"/>, allowing different Ollama models to be configured
/// per reasoning role (Planner/Coach/Insights/Default).
/// </summary>
public class OllamaLLMService(
    HttpClient httpClient,
    IOptions<OllamaSettings> options,
    IModelSelectionService modelSelectionService,
    ILogger<OllamaLLMService> logger) : ILLMService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly OllamaSettings _settings = options.Value;
    private readonly IModelSelectionService _modelSelectionService = modelSelectionService;
    private readonly ILogger<OllamaLLMService> _logger = logger;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T> GenerateStructuredResponseAsync<T>(string systemPrompt, string userPrompt, ModelType modelType = ModelType.Default)
    {
        var jsonInstruction = "You must respond with ONLY a single valid JSON object. Do not include markdown " +
            "code fences, explanations, or any text outside the JSON object.";
        var combinedSystemPrompt = $"{systemPrompt}\n\n{jsonInstruction}";

        var rawResponse = await CallOllamaAsync(combinedSystemPrompt, userPrompt, useJsonFormat: true, modelType);

        var json = ExtractJson(rawResponse);
        if (json is null)
        {
            throw new InvalidOperationException(
                $"Ollama response did not contain a parseable JSON object. Raw response: {Truncate(rawResponse)}");
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(json, SerializerOptions);
            if (result is null)
            {
                throw new InvalidOperationException("Deserialized structured response was null.");
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize Ollama JSON response into {typeof(T).Name}. Raw response: {Truncate(rawResponse)}", ex);
        }
    }

    public async Task<string> GenerateTextResponseAsync(string systemPrompt, string userPrompt, ModelType modelType = ModelType.Default)
    {
        return await CallOllamaAsync(systemPrompt, userPrompt, useJsonFormat: false, modelType);
    }

    private async Task<string> CallOllamaAsync(string systemPrompt, string userPrompt, bool useJsonFormat, ModelType modelType)
    {
        var model = _modelSelectionService.ResolveModel(modelType);

        // Some structured responses (e.g. executive briefings with several string arrays)
        // exceed Ollama's default num_predict token cap, causing generation to stop
        // mid-JSON and produce an unparseable/truncated object. MaxResponseTokens is
        // configurable via Ollama:MaxResponseTokens (set to -1 to remove the cap entirely).
        var modelOptions = new Dictionary<string, object?> { ["num_predict"] = _settings.MaxResponseTokens };
        if (_settings.ContextWindowTokens is { } contextWindowTokens)
        {
            modelOptions["num_ctx"] = contextWindowTokens;
        }

        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["prompt"] = userPrompt,
            ["system"] = systemPrompt,
            ["stream"] = false,
            ["options"] = modelOptions
        };

        if (useJsonFormat)
        {
            requestBody["format"] = "json";
        }

        using var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/generate") { Content = content };

        var apiKey = string.IsNullOrWhiteSpace(_settings.ApiKey)
            ? Environment.GetEnvironmentVariable("OLLAMA_API_KEY")
            : _settings.ApiKey;

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }

        try
        {
            using var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var isCloudModel = model?.Contains(":cloud", StringComparison.OrdinalIgnoreCase) == true;
                var message = isCloudModel
                    ? $"Ollama returned 403 Forbidden for cloud model '{model}'. Cloud-hosted models require " +
                      "authentication - either run 'ollama signin' on the machine hosting Ollama, or set " +
                      "Ollama:ApiKey in configuration (or the OLLAMA_API_KEY environment variable). " +
                      "Alternatively, configure a locally pulled model (e.g. 'qwen3:8b') to run fully offline."
                    : $"Ollama returned 403 Forbidden for model '{model}'.";

                _logger.LogError(message);
                throw new InvalidOperationException(message);
            }

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseBody);

            return document.RootElement.TryGetProperty("response", out var responseElement)
                ? responseElement.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach Ollama at {BaseUrl} for model {Model}.", _settings.BaseUrl, model);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Ollama request timed out for model {Model}.", model);
            throw;
        }
    }

    /// <summary>
    /// Extracts the first top-level JSON object from a raw model response, tolerating
    /// surrounding reasoning text (e.g. "&lt;think&gt;" blocks) or markdown code fences that
    /// local models sometimes emit despite JSON-mode instructions.
    /// </summary>
    private static string? ExtractJson(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return null;
        }

        var jsonStart = rawResponse.IndexOf('{');
        var jsonEnd = rawResponse.LastIndexOf('}');

        return jsonStart >= 0 && jsonEnd > jsonStart
            ? rawResponse[jsonStart..(jsonEnd + 1)]
            : null;
    }

    private static string Truncate(string value, int maxLength = 500) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}

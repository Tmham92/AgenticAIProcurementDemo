namespace ProcurementConcierge.Api.Configuration;

/// <summary>
/// Connection settings for the local Ollama runtime, bound from the "Ollama" section of
/// appsettings.json. Supports configuring different models per reasoning role
/// (Planner/Coach/Insights), falling back to <see cref="DefaultModel"/> when a
/// role-specific model is not configured. <see cref="Model"/> is retained for backward
/// compatibility with existing configuration and is used as a final fallback.
/// </summary>
public class OllamaSettings
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Legacy single-model setting, retained for backward compatibility. Prefer
    /// <see cref="DefaultModel"/> for new configuration.
    /// </summary>
    public string Model { get; set; } = "qwen3:8b";

    public string? DefaultModel { get; set; }
    public string? PlannerModel { get; set; }
    public string? InsightModel { get; set; }
    public string? CoachingModel { get; set; }

    /// <summary>
    /// Optional API key used to authenticate with Ollama when using cloud-hosted models
    /// (model tags ending in ":cloud"), sent as a Bearer token. Not required for purely
    /// local models. Can also be supplied via the OLLAMA_API_KEY environment variable.
    /// </summary>
    public string? ApiKey { get; set; }
}

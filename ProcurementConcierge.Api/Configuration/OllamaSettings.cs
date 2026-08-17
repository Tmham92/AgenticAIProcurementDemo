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

    /// <summary>
    /// Maximum number of tokens Ollama is allowed to generate per response (the "options.num_predict"
    /// request parameter). Structured responses with several string arrays (e.g. executive
    /// briefings) can be long; too low a cap truncates the JSON mid-generation and causes
    /// deserialization failures. Defaults to 4096. Set to -1 to remove the cap entirely
    /// (model stops naturally at end-of-response instead of an arbitrary token limit).
    /// </summary>
    public int MaxResponseTokens { get; set; } = 4096;

    /// <summary>
    /// Context window size in tokens (the "options.num_ctx" request parameter), covering the
    /// combined system prompt, user prompt, and generated response. If unset, Ollama's model
    /// default is used. Increase this if prompts + expected output are large enough to be
    /// truncated even with a generous <see cref="MaxResponseTokens"/>.
    /// </summary>
    public int? ContextWindowTokens { get; set; }
}

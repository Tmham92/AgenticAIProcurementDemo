namespace ProcurementConcierge.Api.Models;

/// <summary>
/// Health check result for the local Ollama LLM runtime, returned by
/// GET /api/system/ollama-status.
/// </summary>
public class OllamaStatus
{
    public bool Connected { get; set; }
    public string Model { get; set; } = string.Empty;
    public long LatencyMs { get; set; }
    public string? Error { get; set; }
}

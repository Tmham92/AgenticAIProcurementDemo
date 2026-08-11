using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Verifies connectivity and readiness of the underlying LLM provider (e.g. local Ollama),
/// independent of the <see cref="ILLMService"/> reasoning abstraction, so operational
/// health can be checked without exercising business prompts.
/// </summary>
public interface IModelHealthCheckService
{
    Task<OllamaStatus> CheckStatusAsync();
}

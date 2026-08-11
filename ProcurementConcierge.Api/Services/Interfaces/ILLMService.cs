using ProcurementConcierge.Api.Configuration;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Provider-agnostic abstraction for the AI reasoning layer. All agents and services that
/// need LLM-powered reasoning (request analysis, coaching, planning, reflection, executive
/// summarization) must depend on this interface rather than any specific provider (Ollama,
/// Azure OpenAI, etc.), so the underlying model/provider can be swapped via dependency
/// injection alone, with no changes to business logic or agent code.
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// Prompts the model to produce a structured JSON response and deserializes it into
    /// <typeparamref name="T"/>. Implementations must instruct the model to return valid
    /// JSON only and handle malformed/extraneous output robustly.
    /// </summary>
    /// <param name="modelType">
    /// The reasoning role requesting the response (e.g. Planner, Coach, Insights), used by
    /// the implementation to resolve which underlying model to use. Defaults to
    /// <see cref="ModelType.Default"/> when the caller does not need a specific role.
    /// </param>
    Task<T> GenerateStructuredResponseAsync<T>(string systemPrompt, string userPrompt, ModelType modelType = ModelType.Default);

    /// <summary>
    /// Prompts the model for a free-form natural language text response.
    /// </summary>
    /// <param name="modelType">
    /// The reasoning role requesting the response, used to resolve which underlying model
    /// to use. Defaults to <see cref="ModelType.Default"/>.
    /// </param>
    Task<string> GenerateTextResponseAsync(string systemPrompt, string userPrompt, ModelType modelType = ModelType.Default);
}

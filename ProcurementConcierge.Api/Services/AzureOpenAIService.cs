using ProcurementConcierge.Api.Configuration;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Placeholder <see cref="ILLMService"/> implementation for a future Azure OpenAI-backed
/// AI Reasoning Layer. Swapping the local <see cref="OllamaLLMService"/> for this provider
/// is intended to require only a Dependency Injection registration change in
/// <c>Program.cs</c> (e.g. <c>AddScoped&lt;ILLMService, AzureOpenAIService&gt;()</c>) -
/// no changes to any agent or business service that consumes <see cref="ILLMService"/>.
/// The <see cref="ModelType"/> parameter is expected to eventually resolve to an Azure
/// OpenAI deployment name per reasoning role, mirroring <see cref="ModelSelectionService"/>.
/// </summary>
public class AzureOpenAIService : ILLMService
{
    public Task<T> GenerateStructuredResponseAsync<T>(string systemPrompt, string userPrompt, ModelType modelType = ModelType.Default)
    {
        throw new NotImplementedException(
            "AzureOpenAIService is a placeholder for future migration and is not yet implemented. " +
            "Configure and register OllamaLLMService as ILLMService instead.");
    }

    public Task<string> GenerateTextResponseAsync(string systemPrompt, string userPrompt, ModelType modelType = ModelType.Default)
    {
        throw new NotImplementedException(
            "AzureOpenAIService is a placeholder for future migration and is not yet implemented. " +
            "Configure and register OllamaLLMService as ILLMService instead.");
    }
}

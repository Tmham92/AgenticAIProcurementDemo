using Microsoft.Extensions.Options;
using ProcurementConcierge.Api.Configuration;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Resolves the Ollama model name to use for a given reasoning role from
/// <see cref="OllamaSettings"/>. Falls back to the default model (or the legacy
/// <see cref="OllamaSettings.Model"/> setting) when a role-specific model is not
/// configured, so existing configuration keeps working unchanged.
/// </summary>
public class ModelSelectionService(IOptions<OllamaSettings> options) : IModelSelectionService
{
    private readonly OllamaSettings _settings = options.Value;

    public string ResolveModel(ModelType modelType)
    {
        var resolved = modelType switch
        {
            ModelType.Planner => _settings.PlannerModel,
            ModelType.Coach => _settings.CoachingModel,
            ModelType.Insights => _settings.InsightModel,
            ModelType.Default => _settings.DefaultModel,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(resolved))
        {
            return resolved;
        }

        if (!string.IsNullOrWhiteSpace(_settings.DefaultModel))
        {
            return _settings.DefaultModel;
        }

        return _settings.Model;
    }
}

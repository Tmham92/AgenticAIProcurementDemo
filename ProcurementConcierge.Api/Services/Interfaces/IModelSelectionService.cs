using ProcurementConcierge.Api.Configuration;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Resolves which model name should be used for a given reasoning role
/// (<see cref="ModelType"/>), decoupling agents/services from configuration details and
/// preparing the architecture for future multi-model/Azure OpenAI deployment switching.
/// </summary>
public interface IModelSelectionService
{
    /// <summary>
    /// Returns the configured model name for the given reasoning role, falling back to the
    /// default model when a role-specific model has not been configured.
    /// </summary>
    string ResolveModel(ModelType modelType);
}

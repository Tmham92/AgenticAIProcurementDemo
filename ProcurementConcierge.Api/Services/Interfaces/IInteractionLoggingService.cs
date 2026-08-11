using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Persists interaction insight records for every analyzed procurement request and
/// allows retrieving the recorded history.
/// </summary>
public interface IInteractionLoggingService
{
    Task LogInteractionAsync(InteractionRecord record);

    Task<List<InteractionRecord>> GetAllAsync();
}

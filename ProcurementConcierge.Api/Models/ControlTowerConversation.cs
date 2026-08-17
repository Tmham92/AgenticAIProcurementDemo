namespace ProcurementConcierge.Api.Models;

/// <summary>
/// A single persisted Control Tower Chat exchange (question + answer), stored so
/// leadership conversation history survives across sessions.
/// </summary>
public class ControlTowerConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

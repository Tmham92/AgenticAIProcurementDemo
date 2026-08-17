namespace ProcurementConcierge.Contracts;

/// <summary>
/// A persisted Control Tower Chat conversation turn (question + answer), returned by the
/// chat history endpoint for display in the Blazor Control Tower Chat page.
/// </summary>
public class ControlTowerConversationEntry
{
    public Guid Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

namespace ProcurementConcierge.Contracts;

/// <summary>
/// An executive's natural language question posed to the Control Tower Chat.
/// </summary>
public class ControlTowerQuestion
{
    public string Question { get; set; } = string.Empty;
}

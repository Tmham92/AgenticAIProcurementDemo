namespace ProcurementConcierge.Contracts;

/// <summary>
/// Represents the incoming natural language procurement request from a user.
/// </summary>
public class ProcurementRequest
{
    public string Message { get; set; } = string.Empty;
}

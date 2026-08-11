namespace ProcurementConcierge.Contracts;

/// <summary>
/// Simulated Coupa supplier record used to mimic a preferred-supplier lookup that would
/// otherwise come from a real Coupa integration.
/// </summary>
public class Supplier
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string Rating { get; set; } = string.Empty;
}

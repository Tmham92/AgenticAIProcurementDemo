namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// The result of a single agent's execution: whether it succeeded, a human-readable
/// summary of what it did, and any outputs it produced (also mirrored into the shared
/// <see cref="AgentRunContext.WorkingMemory"/>).
/// </summary>
public class AgentExecutionStepResult
{
    public bool Success { get; set; }
    public string Summary { get; set; } = string.Empty;
    public Dictionary<string, object> Outputs { get; set; } = [];
}

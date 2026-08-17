namespace ProcurementConcierge.Contracts;

/// <summary>
/// A single structured entry in the agentic reasoning path, capturing which service acted,
/// what action it performed, the input it received, the output it produced, and when it ran.
/// Used to render the full agent execution/reasoning path in the UI, making the Procurement
/// Concierge behave as a transparent, auditable Agentic Procurement Adoption Layer rather
/// than an opaque chatbot.
/// </summary>
public class AgentExecutionContext
{
    public int StepNumber { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The Dynamic Replanning iteration (1-based) during which this step ran, so the UI
    /// can group the reasoning path by iteration (Iteration 1, Iteration 2, ...).
    /// </summary>
    public int IterationNumber { get; set; } = 1;
}

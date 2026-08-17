namespace ProcurementConcierge.Contracts;

/// <summary>
/// A single structured step in the agentic execution pipeline, used to build a
/// transparent, auditable execution log.
/// </summary>
public class ExecutionStepDetail
{
    public int StepNumber { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The Dynamic Replanning iteration (1-based) during which this step ran.
    /// </summary>
    public int IterationNumber { get; set; } = 1;
}

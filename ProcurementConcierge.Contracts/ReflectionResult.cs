namespace ProcurementConcierge.Contracts;

/// <summary>
/// The Reflection Agent's evaluation of whether the orchestrated run actually achieved the
/// user's goal, along with any missing information and an overall confidence score.
/// </summary>
public class ReflectionResult
{
    public bool GoalAchieved { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> MissingInformation { get; set; } = [];
    public double Confidence { get; set; }

    /// <summary>
    /// LLM-generated narrative summary of the reflection (what happened, what's missing,
    /// whether human review is warranted). Falls back to <see cref="Reason"/> when the
    /// LLM is unavailable.
    /// </summary>
    public string ReflectionSummary { get; set; } = string.Empty;

    /// <summary>
    /// Suggested next action following reflection (e.g. "Proceed", "Request clarification
    /// on country", "Escalate to ProcOps").
    /// </summary>
    public string RecommendedNextAction { get; set; } = string.Empty;

    /// <summary>
    /// Whether the reflection step determined human intervention is required.
    /// </summary>
    public bool HumanInterventionRequired { get; set; }

    /// <summary>
    /// Set when reflection determines that critical information is missing or too
    /// uncertain to safely proceed, prompting the concierge to ask the user a
    /// follow-up question instead of generating recommendations.
    /// </summary>
    public ClarificationRequest? Clarification { get; set; }

    /// <summary>
    /// True when the user must be asked a clarifying follow-up question before the
    /// orchestrator can continue (mirrors <see cref="Clarification"/>'s required flag).
    /// </summary>
    public bool RequiresClarification { get; set; }

    /// <summary>
    /// True when the orchestrator should generate and execute an additional follow-up
    /// plan (Dynamic Replanning) rather than returning the current result, because the
    /// goal was not achieved, required data is still missing from working memory, and
    /// the iteration budget has not been exhausted.
    /// </summary>
    public bool RequiresReplanning { get; set; }
}

namespace ProcurementConcierge.Contracts;

/// <summary>
/// Extremely short, navigation-style guidance whose only purpose is to get the employee
/// to the correct Coupa workflow with the least possible effort. This intentionally
/// excludes any procurement documentation, business justification, or supplier
/// requirement content - it answers only: where do I stand, what do I do next, what
/// approval applies, and how can I make it easier.
/// </summary>
public class UserGuidanceResponse
{
    /// <summary>Current status in five words or fewer (e.g. "Ready to proceed").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The single next action, in ten words or fewer (e.g. "Create Coupa requisition").</summary>
    public string NextAction { get; set; } = string.Empty;

    /// <summary>Required approval, in five words or fewer (e.g. "Manager approval").</summary>
    public string Approval { get; set; } = string.Empty;

    /// <summary>A helpful tip, in ten words or fewer (e.g. "Add delivery location").</summary>
    public string Tip { get; set; } = string.Empty;

    /// <summary>
    /// 0-100 score answering "how easy is this request to process?" (100 = can proceed
    /// immediately, 0 = escalated), deterministically derived from <see cref="GuidanceLevel"/>.
    /// </summary>
    public int AdoptionFriendlinessScore { get; set; }

    /// <summary>Deterministic friction classification driving the UI badge color.</summary>
    public GuidanceLevel GuidanceLevel { get; set; }

    /// <summary>
    /// Concise references (e.g. "Country Procedure: EMEA Travel SOP") to the most relevant
    /// procurement documentation retrieved by the KnowledgeAgent, if any. Kept short and
    /// optional so it does not dilute the ultra-short guidance above.
    /// </summary>
    public List<string> KnowledgeReferences { get; set; } = [];
}

namespace ProcurementConcierge.Contracts;

/// <summary>
/// Coaching feedback generated before policy evaluation, helping the user improve a
/// procurement request's completeness before it is evaluated against policy.
/// </summary>
public class ProcurementCoachingAssessment
{
    public int RequestQualityScore { get; set; }

    /// <summary>
    /// How clear/unambiguous the request's language is (0-100), as judged by the LLM.
    /// </summary>
    public int ClarityScore { get; set; }

    /// <summary>
    /// Overall procurement readiness (0-100): whether the request has enough information
    /// to proceed through policy/compliance evaluation without further clarification.
    /// </summary>
    public int ProcurementReadinessScore { get; set; }

    public List<string> MissingInformation { get; set; } = [];
    public List<string> ImprovementSuggestions { get; set; } = [];
}

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Well-known <see cref="AgentRunContext.WorkingMemory"/> key names, centralized to avoid
/// magic strings scattered across agent implementations.
/// </summary>
public static class MemoryKeys
{
    public const string OriginalMessage = "OriginalMessage";
    public const string Analysis = "Analysis";
    public const string Policy = "Policy";
    public const string CountryRule = "CountryRule";
    public const string PolicyDeviations = "PolicyDeviations";
    public const string ComplianceScoreResult = "ComplianceScoreResult";
    public const string ComplianceEvaluation = "ComplianceEvaluation";
    public const string ProcOpsAssessment = "ProcOpsAssessment";
    public const string Recommendation = "Recommendation";
    public const string RecommendedNextAction = "RecommendedNextAction";
    public const string ProcessDiscoveryFindings = "ProcessDiscoveryFindings";
    public const string GovernanceAssessment = "GovernanceAssessment";
}

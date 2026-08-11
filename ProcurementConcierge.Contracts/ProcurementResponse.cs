namespace ProcurementConcierge.Contracts;

/// <summary>
/// Final structured response returned by the Procurement Concierge API.
/// </summary>
public class ProcurementResponse
{
    public string Category { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal EstimatedSpend { get; set; }
    public List<string> PreferredSuppliers { get; set; } = new();
    public string RequiredApproval { get; set; } = string.Empty;
    public string ComplianceStatus { get; set; } = string.Empty;
    public string ComplianceRisk { get; set; } = string.Empty;
    public bool PolicyDeviation { get; set; }
    public string PolicyDeviationReason { get; set; } = string.Empty;
    public string CountryGuidance { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string RecommendedNextAction { get; set; } = string.Empty;

    // --- Feature 1: Confidence scoring ---
    public int CategoryConfidence { get; set; }
    public int CountryConfidence { get; set; }
    public int SpendConfidence { get; set; }
    public bool NeedsHumanReview { get; set; }

    // --- Feature 2: Policy deviation engine ---
    public List<PolicyDeviationDetail> PolicyDeviations { get; set; } = new();

    // --- Feature 3: Compliance scoring ---
    public int ComplianceScore { get; set; }
    public string ComplianceLevel { get; set; } = string.Empty;

    // --- Feature 7: Human-in-the-loop escalation ---
    public string EscalationLevel { get; set; } = string.Empty;
    public string EscalationReason { get; set; } = string.Empty;

    /// <summary>
    /// Request Quality Assessment: coaches the user towards a more complete, compliant request.
    /// </summary>
    public RequestQualityAssessment RequestQuality { get; set; } = new();

    /// <summary>
    /// Procurement Coaching feedback generated before policy evaluation, displayed ahead of
    /// compliance results in the UI.
    /// </summary>
    public ProcurementCoachingAssessment ProcurementCoaching { get; set; } = new();

    /// <summary>
    /// Assessment of whether this request could have required ProcOps intervention.
    /// </summary>
    public ProcOpsAssessment ProcOpsAssessment { get; set; } = new();

    /// <summary>
    /// Simulated Coupa integration result (preferred suppliers, draft requisition, and
    /// approval route), demonstrating what would happen if this request were submitted to
    /// Coupa without requiring a real Coupa connection.
    /// </summary>
    public CoupaSimulationResult CoupaSimulation { get; set; } = new();

    /// <summary>
    /// Ordered log of the agentic planning steps executed to produce this response.
    /// </summary>
    public List<string> ExecutionSteps { get; set; } = new();

    /// <summary>
    /// Structured execution log (Feature 8) with per-step service name, action, outcome, and timestamp.
    /// </summary>
    public List<ExecutionStepDetail> ExecutionStepDetails { get; set; } = new();

    /// <summary>
    /// The full agentic reasoning path: an ordered trace of every service invoked to produce
    /// this response, including the input it received and the output it produced. Rendered in
    /// the UI so the Procurement Concierge behaves as a transparent Agentic Procurement
    /// Adoption Layer rather than an opaque chatbot.
    /// </summary>
    public List<AgentExecutionContext> AgentExecutionPath { get; set; } = new();
}

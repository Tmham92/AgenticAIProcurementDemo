using ProcurementConcierge.Api.Agents;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;
using InteractionRecord = ProcurementConcierge.Api.Models.InteractionRecord;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Agent Orchestrator for the Procurement Adoption Concierge. Rather than behaving as a
/// chatbot, this service coordinates a fixed sequence of specialized agents/services and
/// records a fully transparent, auditable reasoning path (<see cref="AgentExecutionContext"/>)
/// for every request:
///
/// Step 1: ProcurementCoachingService  - coach the user towards a more complete request
/// Step 2: RequestAnalysisService      - extract category/country/spend and confidence
/// Step 3: PolicyService                - retrieve global policy, country guidance, deviations, and compliance score
/// Step 4: ComplianceService            - evaluate preferred supplier availability, required approval, and risk
/// Step 5: ProcOpsDependencyService      - assess whether ProcOps intervention would be required
/// Step 6: RecommendationService        - generate procurement guidance and recommended next action
/// Step 7: InteractionLoggingService    - persist an interaction record for long-term reporting
/// Step 8: ExecutiveInsightService       - synthesize updated executive-level insights
///
/// Additional <see cref="IProcurementAgent"/> implementations (e.g. ComplianceAgent,
/// ProcessDiscoveryAgent, SpendIntelligenceAgent, ProcurementControlTowerAgent) are executed
/// after the core steps, allowing the orchestrated workflow to be extended without modifying
/// this orchestrator.
///
/// The goal of this pipeline is NOT to create purchase orders - it exists to guide users
/// towards the correct procurement process, detect governance risks, and reduce dependency
/// on procurement operations teams.
/// </summary>
public class ProcurementConciergeService(
    IProcurementCoachingService procurementCoachingService,
    IAgentOrchestrator agentOrchestrator,
    IRequestQualityService requestQualityService,
    IRequestHistoryStore requestHistoryStore,
    IInteractionLoggingService interactionLoggingService,
    IExecutiveInsightService executiveInsightService,
    ICoupaSimulationService coupaSimulationService,
    IEnumerable<IProcurementAgent> additionalAgents,
    ILogger<ProcurementConciergeService> logger) : IProcurementConciergeService
{
    private readonly IProcurementCoachingService _procurementCoachingService = procurementCoachingService;
    private readonly IAgentOrchestrator _agentOrchestrator = agentOrchestrator;
    private readonly IRequestQualityService _requestQualityService = requestQualityService;
    private readonly IRequestHistoryStore _requestHistoryStore = requestHistoryStore;
    private readonly IInteractionLoggingService _interactionLoggingService = interactionLoggingService;
    private readonly IExecutiveInsightService _executiveInsightService = executiveInsightService;
    private readonly ICoupaSimulationService _coupaSimulationService = coupaSimulationService;
    private readonly IEnumerable<IProcurementAgent> _additionalAgents = additionalAgents;
    private readonly ILogger<ProcurementConciergeService> _logger = logger;

    public async Task<ProcurementResponse> ProcessRequestAsync(ProcurementRequest request)
    {
        var response = new ProcurementResponse();
        var stepNumber = 0;

        // The Agent Orchestrator plans and executes the core reasoning agents (request
        // analysis, policy, country guidance, compliance, ProcOps dependency, and
        // recommendation) dynamically, rather than as a fixed sequence. Its execution
        // timeline is folded into this response's reasoning path below.
        var executionResult = await _agentOrchestrator.RunAsync(new AgentGoal
        {
            UserRequest = request.Message,
            GoalDescription = "Guide the user towards the correct procurement process and evaluate compliance risk."
        });

        var analysis = GetOutput<ProcurementAnalysis>(executionResult, MemoryKeys.Analysis)
            ?? new ProcurementAnalysis { OriginalMessage = request.Message };
        var policy = GetOutput<ProcurementPolicy>(executionResult, MemoryKeys.Policy);
        var countryRule = GetOutput<CountryRule>(executionResult, MemoryKeys.CountryRule);
        var deviations = GetOutput<List<PolicyDeviationDetail>>(executionResult, MemoryKeys.PolicyDeviations) ?? [];
        var scoreResult = GetOutput<ComplianceScoreResult>(executionResult, MemoryKeys.ComplianceScoreResult) ?? new ComplianceScoreResult();
        var evaluation = GetOutput<ComplianceEvaluation>(executionResult, MemoryKeys.ComplianceEvaluation) ?? new ComplianceEvaluation();
        var procOpsAssessment = GetOutput<ProcOpsAssessment>(executionResult, MemoryKeys.ProcOpsAssessment) ?? new ProcOpsAssessment();
        var recommendation = GetOutput<string>(executionResult, MemoryKeys.Recommendation) ?? string.Empty;
        var nextAction = GetOutput<string>(executionResult, MemoryKeys.RecommendedNextAction) ?? string.Empty;
        var guidance = GetOutput<UserGuidanceResponse>(executionResult, MemoryKeys.UserGuidance) ?? new UserGuidanceResponse();
        var escalation = GetOutput<EscalationDecision>(executionResult, nameof(EscalationDecision))
            ?? new EscalationDecision { Level = EscalationLevel.Automatic, Reason = "No escalation data available." };

        response.ProcOpsAssessment = procOpsAssessment;

        // Step 1: Coach the user towards a more complete request, before policy evaluation.
        var coachingAssessment = await _procurementCoachingService.CoachAsync(analysis);
        response.ProcurementCoaching = coachingAssessment;
        RecordStep(response, ref stepNumber, nameof(ProcurementCoachingService), "Coach Request Quality",
            input: request.Message,
            output: $"Score: {coachingAssessment.RequestQualityScore}, Missing: {coachingAssessment.MissingInformation.Count}");

        // Steps 2-6: Fold the Agent Orchestrator's own execution timeline (dynamically
        // planned request analysis, policy, country guidance, compliance, ProcOps
        // dependency, and recommendation agents) into this response's reasoning path.
        foreach (var orchestratedStep in executionResult.Steps)
        {
            RecordStep(response, ref stepNumber, orchestratedStep.AgentName, orchestratedStep.Reason,
                input: request.Message,
                output: orchestratedStep.Outcome);
        }

        response.Category = analysis.Category;
        response.Country = analysis.Country;
        response.EstimatedSpend = analysis.EstimatedSpend;
        response.PreferredSuppliers = policy?.PreferredSuppliers ?? [];
        response.RequiredApproval = evaluation.RequiredApproval;
        response.ComplianceStatus = evaluation.ComplianceStatus;
        response.ComplianceRisk = evaluation.ComplianceRisk;
        response.PolicyDeviation = evaluation.IsPolicyDeviation;
        response.PolicyDeviationReason = evaluation.PolicyDeviationReason;
        response.CountryGuidance = countryRule?.Guidance ?? string.Empty;
        response.Recommendation = recommendation;
        response.RecommendedNextAction = nextAction;
        response.Guidance = guidance;

        response.CategoryConfidence = analysis.CategoryConfidence;
        response.CountryConfidence = analysis.CountryConfidence;
        response.SpendConfidence = analysis.SpendConfidence;
        response.NeedsHumanReview = analysis.NeedsHumanReview;

        response.PolicyDeviations = deviations;
        response.ComplianceScore = scoreResult.Score;
        response.ComplianceLevel = scoreResult.Level;

        response.EscalationLevel = escalation.Level.ToString();
        response.EscalationReason = escalation.Reason;

        // Assess request quality (used by dashboards/coaching alongside the pre-policy coaching step).
        var qualityAssessment = _requestQualityService.Assess(analysis, policy);
        response.RequestQuality = qualityAssessment;

        // Simulate creating this request in Coupa (preferred suppliers, draft requisition, approval route).
        var coupaSuppliers = await _coupaSimulationService.GetPreferredSuppliers(analysis.Category);
        var coupaRequisition = await _coupaSimulationService.CreateDraftRequisition(new CreateRequisitionRequest
        {
            Category = analysis.Category,
            Country = analysis.Country,
            EstimatedSpend = analysis.EstimatedSpend,
            PreferredSupplierName = coupaSuppliers.FirstOrDefault()?.Name
        });
        var coupaApprovalRoute = await _coupaSimulationService.GetApprovalRoute(analysis.Category, analysis.EstimatedSpend);
        response.CoupaSimulation = new CoupaSimulationResult
        {
            PreferredSuppliers = coupaSuppliers,
            DraftRequisition = coupaRequisition,
            ApprovalRoute = coupaApprovalRoute
        };

        // Record the request in the legacy in-memory history store for adoption insights and process discovery.
        _requestHistoryStore.Record(new RequestHistoryEntry
        {
            Category = analysis.Category,
            Country = analysis.Country,
            ComplianceScore = scoreResult.Score,
            ComplianceLevel = scoreResult.Level,
            Deviations = deviations
        });

        // Step 7: Persist an interaction insight record to the local SQLite database for long-term reporting.
        await _interactionLoggingService.LogInteractionAsync(new InteractionRecord
        {
            OriginalRequest = request.Message,
            Category = analysis.Category,
            Country = analysis.Country,
            EstimatedSpend = analysis.EstimatedSpend,
            ComplianceScore = scoreResult.Score,
            ComplianceLevel = scoreResult.Level,
            PolicyDeviation = evaluation.IsPolicyDeviation,
            DeviationTypes = deviations.Select(d => d.Type).ToList(),
            Recommendation = recommendation
        });
        RecordStep(response, ref stepNumber, nameof(InteractionLoggingService), "Log Interaction",
            input: $"Category: {analysis.Category}, Compliance Score: {scoreResult.Score}",
            output: "Recorded to SQLite interaction log");

        // Step 8: Synthesize updated executive-level insights across all persisted interactions.
        var executiveInsights = await _executiveInsightService.GetInsightsAsync();
        RecordStep(response, ref stepNumber, nameof(ExecutiveInsightService), "Synthesize Executive Insights",
            input: "All persisted interaction records",
            output: $"{executiveInsights.TopFindings.Count} top finding(s) synthesized");

        // Future extension point: run any additional registered agents (ComplianceAgent,
        // ProcessDiscoveryAgent, SpendIntelligenceAgent, ProcurementControlTowerAgent, etc.)
        foreach (var agent in _additionalAgents)
        {
            RecordStep(response, ref stepNumber, agent.AgentName, "Execute Agent", input: "Analysis & Policy", output: "Executed");
            await agent.ExecuteAsync(analysis, policy, response);
        }

        return response;
    }

    private void RecordStep(ProcurementResponse response, ref int stepNumber, string serviceName, string action, string input, string output)
    {
        stepNumber++;
        var context = new AgentExecutionContext
        {
            StepNumber = stepNumber,
            ServiceName = serviceName,
            Action = action,
            Input = input,
            Output = output
        };

        response.AgentExecutionPath.Add(context);

        var detail = new ExecutionStepDetail
        {
            StepNumber = stepNumber,
            ServiceName = serviceName,
            Action = action,
            Outcome = output,
            Timestamp = context.Timestamp
        };
        response.ExecutionStepDetails.Add(detail);
        response.ExecutionSteps.Add($"Step {stepNumber}: {serviceName} - {action}. Outcome: {output}");

        _logger.LogInformation("Step {StepNumber}: {ServiceName} - {Action}. Input: {Input}. Output: {Output}",
            stepNumber, serviceName, action, input, output);
    }

    private static T? GetOutput<T>(AgentExecutionResult executionResult, string key)
    {
        return executionResult.Outputs.TryGetValue(key, out var value) && value is T typed ? typed : default;
    }
}

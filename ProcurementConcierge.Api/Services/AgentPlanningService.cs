using System.Text.Json;
using System.Text.Json.Serialization;
using ProcurementConcierge.Api.Configuration;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// AI-driven planner for the Agent Orchestrator. Given a user's natural language request,
/// uses the AI Reasoning Layer (<see cref="ILLMService"/>) to determine which of the
/// available agents are required, in what sequence, and why - rather than always running
/// a fixed pipeline.
/// </summary>
public class AgentPlanningService(ILLMService llmService, ILogger<AgentPlanningService> logger) : IAgentPlanningService
{
    private readonly ILLMService _llmService = llmService;
    private readonly ILogger<AgentPlanningService> _logger = logger;

    private static readonly string[] AvailableAgents =
    [
        "RequestAnalysisAgent",
        "PolicyAgent",
        "ComplianceAgent",
        "CountryGuidanceAgent",
        "ProcOpsDependencyAgent",
        "RecommendationAgent",
        "ProcessDiscoveryAgent",
        "GovernanceAgent"
    ];

    private const string SystemPrompt = """
        You are the planning module of an agentic procurement adoption platform. Given a user's
        request, decide which of the following agents are required to satisfy the goal, in what
        order, and why. Do not include agents that are not needed.

        Available agents:
        - RequestAnalysisAgent: extracts category, country, and estimated spend from the request text.
        - PolicyAgent: retrieves global procurement policy and detects policy deviations. Requires RequestAnalysisAgent to have already run.
        - CountryGuidanceAgent: retrieves country-specific procurement guidance. Requires RequestAnalysisAgent to have already run.
        - ComplianceAgent: evaluates preferred supplier availability, required approval, and compliance score/risk. Requires PolicyAgent to have already run.
        - ProcOpsDependencyAgent: assesses whether Procurement Operations intervention would be required. Requires ComplianceAgent to have already run.
        - RecommendationAgent: produces the final guidance narrative and recommended next action. Should generally run last when a recommendation is needed.
        - ProcessDiscoveryAgent: analyzes historical interaction patterns (use for questions about trends/history rather than a single new request).
        - GovernanceAgent: assesses broader procurement risk (repeated deviations, high-risk categories, country governance concerns) using historical data.

        Respond with a JSON object matching this exact shape:
        { "goal": string, "tasks": [ { "agentName": string, "reason": string, "priority": number } ] }

        The "priority" field must be a 1-based execution order (1 = first).
        """;

    public async Task<AgentPlan> CreatePlanAsync(string userRequest)
    {
        try
        {
            var userPrompt = $"User request: \"{userRequest}\"";
            var result = await _llmService.GenerateStructuredResponseAsync<PlanResult>(SystemPrompt, userPrompt, ModelType.Planner);

            var plan = ToAgentPlan(result, userRequest);
            if (plan is not null && plan.Tasks.Count > 0)
            {
                return plan;
            }

            _logger.LogWarning("LLM planning response contained no valid tasks. Falling back to heuristic planning.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM planning failed. Falling back to heuristic planning.");
        }

        return FallbackPlan(userRequest);
    }

    private static AgentPlan ToAgentPlan(PlanResult result, string userRequest)
    {
        var plan = new AgentPlan
        {
            Goal = string.IsNullOrWhiteSpace(result.Goal) ? userRequest : result.Goal
        };

        foreach (var task in result.Tasks ?? new List<PlanTaskResult>())
        {
            if (string.IsNullOrWhiteSpace(task.AgentName) || !AvailableAgents.Contains(task.AgentName))
            {
                continue;
            }

            plan.Tasks.Add(new PlannedTask
            {
                AgentName = task.AgentName,
                Reason = task.Reason ?? string.Empty,
                Priority = task.Priority > 0 ? task.Priority : plan.Tasks.Count + 1
            });
        }

        plan.Tasks = plan.Tasks.OrderBy(t => t.Priority).ToList();
        return plan;
    }

    private class PlanResult
    {
        [JsonPropertyName("goal")]
        public string? Goal { get; set; }

        [JsonPropertyName("tasks")]
        public List<PlanTaskResult>? Tasks { get; set; }
    }

    private class PlanTaskResult
    {
        [JsonPropertyName("agentName")]
        public string? AgentName { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("priority")]
        public int Priority { get; set; }
    }

    /// <summary>
    /// Deterministic keyword-based fallback planner, covering the canonical scenarios:
    /// a new procurement request, a question about a rejected requisition, and a
    /// historical/governance question.
    /// </summary>
    private static AgentPlan FallbackPlan(string userRequest)
    {
        var lowered = userRequest.ToLowerInvariant();
        var tasks = new List<PlannedTask>();
        var priority = 1;

        void Add(string agentName, string reason) => tasks.Add(new PlannedTask { AgentName = agentName, Reason = reason, Priority = priority++ });

        var isHistoricalQuestion = lowered.Contains("compliant is") || lowered.Contains("trend") || lowered.Contains("pattern")
            || lowered.Contains("how compliant") || lowered.Contains("over time");
        var isRejectionQuestion = lowered.Contains("reject") || lowered.Contains("denied") || lowered.Contains("procops")
            || lowered.Contains("why was");

        if (isHistoricalQuestion)
        {
            Add("ProcessDiscoveryAgent", "The request asks about historical trends/patterns rather than a new procurement.");
            Add("GovernanceAgent", "Synthesizes governance risk from historical compliance and process discovery data.");
            Add("RecommendationAgent", "Summarizes findings into an actionable recommendation.");
            return new AgentPlan { Goal = userRequest, Tasks = tasks };
        }

        Add("RequestAnalysisAgent", "Extract category, country, and estimated spend from the request.");

        if (isRejectionQuestion)
        {
            Add("ProcOpsDependencyAgent", "The request concerns why a requisition was rejected, which relates to ProcOps dependency.");
            Add("RecommendationAgent", "Provide guidance on how to proceed.");
            return new AgentPlan { Goal = userRequest, Tasks = tasks };
        }

        Add("PolicyAgent", "Retrieve global policy and detect deviations for the extracted category.");
        Add("CountryGuidanceAgent", "Retrieve country-specific guidance for the extracted country.");
        Add("ComplianceAgent", "Evaluate preferred supplier availability, required approval, and compliance score.");
        Add("ProcOpsDependencyAgent", "Assess whether ProcOps intervention would be required.");
        Add("RecommendationAgent", "Produce the final procurement guidance and recommended next action.");

        return new AgentPlan { Goal = userRequest, Tasks = tasks };
    }
}

using ProcurementConcierge.Api.Agents;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Coordinates a single agentic execution run: plans which agents are needed (via
/// <see cref="IAgentPlanningService"/>), executes them in the planned order while sharing
/// state through <see cref="AgentRunContext.WorkingMemory"/>, then reflects on the outcome
/// (<see cref="IReflectionAgent"/>) and decides whether human intervention is required
/// (<see cref="IEscalationAgent"/>), producing a complete, auditable <see cref="AgentExecutionResult"/>.
/// </summary>
public class AgentOrchestrator(
    IAgentPlanningService planningService,
    IEnumerable<IAgent> agents,
    IReflectionAgent reflectionAgent,
    IEscalationAgent escalationAgent,
    ILogger<AgentOrchestrator> logger) : IAgentOrchestrator
{
    private readonly IAgentPlanningService _planningService = planningService;
    private readonly Dictionary<string, IAgent> _agentsByName = agents.ToDictionary(a => a.Name, a => a);
    private readonly IReflectionAgent _reflectionAgent = reflectionAgent;
    private readonly IEscalationAgent _escalationAgent = escalationAgent;
    private readonly ILogger<AgentOrchestrator> _logger = logger;

    public async Task<AgentExecutionResult> RunAsync(AgentGoal goal)
    {
        var context = new AgentRunContext { Goal = goal };
        context.SetMemory(MemoryKeys.OriginalMessage, goal.UserRequest);

        var plan = await _planningService.CreatePlanAsync(goal.UserRequest);

        var stepNumber = 0;
        var allSucceeded = true;

        foreach (var task in plan.Tasks.OrderBy(t => t.Priority))
        {
            if (!_agentsByName.TryGetValue(task.AgentName, out var agent))
            {
                _logger.LogWarning("Planner selected unknown agent '{AgentName}'. Skipping.", task.AgentName);
                continue;
            }

            stepNumber++;
            string outcome;

            try
            {
                var stepResult = await agent.ExecuteAsync(context);
                outcome = stepResult.Summary;
                allSucceeded &= stepResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent '{AgentName}' failed during execution.", agent.Name);
                outcome = $"Failed: {ex.Message}";
                allSucceeded = false;
            }

            context.AddStep(stepNumber, agent.Name, task.Reason, outcome);
        }

        var reflection = await _reflectionAgent.ReflectAsync(context);
        var escalation = _escalationAgent.Decide(context);

        context.SetMemory(nameof(ReflectionResult), reflection);
        context.SetMemory(nameof(EscalationDecision), escalation);

        return new AgentExecutionResult
        {
            Success = allSucceeded && reflection.GoalAchieved,
            Summary = reflection.Reason,
            HumanInterventionRequired = escalation.Level != EscalationLevel.Automatic,
            Steps = context.Steps,
            Outputs = context.WorkingMemory
        };
    }
}

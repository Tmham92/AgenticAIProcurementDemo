using ProcurementConcierge.Api.Agents;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Coordinates a Dynamic Replanning execution run: plans which agents are needed (via
/// <see cref="IAgentPlanningService"/>), executes them while sharing state through
/// <see cref="AgentRunContext.WorkingMemory"/>, then reflects on the outcome
/// (<see cref="IReflectionAgent"/>). If reflection determines the goal was not achieved,
/// critical information is missing, or required data never made it into working memory,
/// <see cref="IReplanningService"/> is consulted to decide whether an additional follow-up
/// plan should be generated and executed. This Plan -> Execute -> Reflect -> Replan loop
/// repeats - bounded by <see cref="ReplanningService.MaxIterations"/> - until the goal is
/// achieved, escalation is required, or the iteration budget is exhausted, producing a
/// complete, auditable <see cref="AgentExecutionResult"/> including the full replanning
/// history.
/// </summary>
public class AgentOrchestrator(
    IAgentPlanningService planningService,
    IEnumerable<IAgent> agents,
    IReflectionAgent reflectionAgent,
    IEscalationAgent escalationAgent,
    IReplanningService replanningService,
    ILogger<AgentOrchestrator> logger) : IAgentOrchestrator
{
    private readonly IAgentPlanningService _planningService = planningService;
    private readonly Dictionary<string, IAgent> _agentsByName = agents.ToDictionary(a => a.Name, a => a);
    private readonly IReflectionAgent _reflectionAgent = reflectionAgent;
    private readonly IEscalationAgent _escalationAgent = escalationAgent;
    private readonly IReplanningService _replanningService = replanningService;
    private readonly ILogger<AgentOrchestrator> _logger = logger;

    public async Task<AgentExecutionResult> RunAsync(AgentGoal goal)
    {
        var context = new AgentRunContext { Goal = goal };
        context.SetMemory(MemoryKeys.OriginalMessage, goal.UserRequest);

        var plan = await _planningService.CreatePlanAsync(goal.UserRequest);

        var state = new ExecutionState();
        var replanningHistory = new List<ReplanningHistory>();

        await ExecutePlanAsync(context, plan, state);

        var reflection = await _reflectionAgent.ReflectAsync(context);
        var escalation = _escalationAgent.Decide(context);

        while (true)
        {
            context.SetMemory(nameof(ReflectionResult), reflection);
            context.SetMemory(nameof(EscalationDecision), escalation);

            // Safety rule: never continue replanning once the request requires human
            // approval - escalate immediately instead of looping further.
            if (escalation.Level == EscalationLevel.ApprovalRequired)
            {
                _logger.LogInformation("Stopping Dynamic Replanning: escalation level is ApprovalRequired.");
                break;
            }

            var replanDecision = _replanningService.Decide(context, reflection);

            if (!replanDecision.ReplanRequired)
            {
                break;
            }

            _logger.LogInformation(
                "Dynamic Replanning iteration {Iteration}/{MaxIterations}: {Reason} Additional agents: {AdditionalTasks}",
                context.CurrentIteration + 1, replanDecision.MaxIterations, replanDecision.Reason, string.Join(", ", replanDecision.AdditionalTasks));

            context.CurrentIteration++;

            replanningHistory.Add(new ReplanningHistory
            {
                Iteration = context.CurrentIteration,
                Reason = replanDecision.Reason,
                AddedAgents = replanDecision.AdditionalTasks
            });

            var followUpPlan = _planningService.CreateFollowUpPlan(replanDecision.AdditionalTasks, goal.GoalDescription);
            await ExecutePlanAsync(context, followUpPlan, state);

            reflection = await _reflectionAgent.ReflectAsync(context);
            escalation = _escalationAgent.Decide(context);

            if (reflection.GoalAchieved || context.CurrentIteration >= replanDecision.MaxIterations)
            {
                context.SetMemory(nameof(ReflectionResult), reflection);
                context.SetMemory(nameof(EscalationDecision), escalation);
                break;
            }
        }

        return new AgentExecutionResult
        {
            Success = state.AllSucceeded && reflection.GoalAchieved,
            Summary = reflection.Reason,
            HumanInterventionRequired = escalation.Level != EscalationLevel.Automatic,
            Steps = context.Steps,
            Outputs = context.WorkingMemory,
            Iterations = context.CurrentIteration,
            ReplanningHistory = replanningHistory
        };
    }

    private async Task ExecutePlanAsync(AgentRunContext context, AgentPlan plan, ExecutionState state)
    {
        foreach (var task in plan.Tasks.OrderBy(t => t.Priority))
        {
            if (!_agentsByName.TryGetValue(task.AgentName, out var agent))
            {
                _logger.LogWarning("Planner selected unknown agent '{AgentName}'. Skipping.", task.AgentName);
                continue;
            }

            state.StepNumber++;
            string outcome;

            try
            {
                var stepResult = await agent.ExecuteAsync(context);
                outcome = stepResult.Summary;
                state.AllSucceeded &= stepResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent '{AgentName}' failed during execution.", agent.Name);
                outcome = $"Failed: {ex.Message}";
                state.AllSucceeded = false;
            }

            context.AddStep(state.StepNumber, agent.Name, task.Reason, outcome);
        }
    }

    /// <summary>
    /// Mutable state carried across the initial plan and every subsequent Dynamic
    /// Replanning follow-up plan within a single run, so step numbering stays sequential
    /// and overall success reflects every agent executed across all iterations.
    /// </summary>
    private class ExecutionState
    {
        public int StepNumber { get; set; }
        public bool AllSucceeded { get; set; } = true;
    }
}

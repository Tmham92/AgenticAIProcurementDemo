namespace ProcurementConcierge.Api.Configuration;

/// <summary>
/// Identifies the reasoning role an agent/service is requesting a model for, allowing
/// different Ollama models (or, in the future, different Azure OpenAI deployments) to be
/// used for different kinds of reasoning tasks.
/// </summary>
public enum ModelType
{
    /// <summary>General-purpose default model, used when no more specific role applies.</summary>
    Default,

    /// <summary>Used by the Agent Orchestrator's planning step (<see cref="Services.AgentPlanningService"/>).</summary>
    Planner,

    /// <summary>Used by procurement coaching (<see cref="Services.ProcurementCoachingService"/>).</summary>
    Coach,

    /// <summary>Used by executive insight/briefing generation (<see cref="Services.ExecutiveInsightService"/>).</summary>
    Insights
}

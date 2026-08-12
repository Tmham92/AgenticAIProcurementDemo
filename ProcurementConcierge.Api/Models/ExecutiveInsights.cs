namespace ProcurementConcierge.Api.Models;

/// <summary>
/// Ranked, executive-level narrative summary of procurement adoption and compliance
/// findings, simulating a future Procurement Control Tower capability.
/// </summary>
public class ExecutiveInsights
{
    public List<string> TopFindings { get; set; } = [];

    /// <summary>
    /// LLM-generated narrative summary of the findings, written as a CPO briefing.
    /// </summary>
    public string ExecutiveSummary { get; set; } = string.Empty;

    public List<string> TopRisks { get; set; } = [];
    public List<string> TopOpportunities { get; set; } = [];
    public List<string> RecommendedActions { get; set; } = [];
}

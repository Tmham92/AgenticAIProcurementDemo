using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Plannable agent added to a follow-up plan by <see cref="Services.Interfaces.IReplanningService"/>
/// whenever reflection determines that critical procurement information (category, country,
/// or estimated spend) is missing or too uncertain to safely proceed. Generates a single,
/// concise follow-up question via <see cref="IClarificationService"/> and stores it in
/// working memory so the orchestrator/concierge service can surface it to the user instead
/// of generating recommendations from incomplete data.
/// </summary>
public class ClarificationAgent(IClarificationService clarificationService) : IAgent
{
    private readonly IClarificationService _clarificationService = clarificationService;

    public string Name => nameof(ClarificationAgent);

    public Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var analysis = context.GetMemory<ProcurementAnalysis>(MemoryKeys.Analysis);
        var missingInformation = new List<string>();

        if (analysis is null || string.IsNullOrWhiteSpace(analysis.Category) || analysis.Category == "Unknown")
        {
            missingInformation.Add("Procurement category");
        }

        if (analysis is null || string.IsNullOrWhiteSpace(analysis.Country) || analysis.Country == "Unknown")
        {
            missingInformation.Add("Country");
        }

        if (analysis is null || analysis.EstimatedSpend <= 0)
        {
            missingInformation.Add("Estimated spend amount");
        }

        if (missingInformation.Count == 0)
        {
            missingInformation.Add("Additional procurement details");
        }

        var followUpQuestion = _clarificationService.GenerateFollowUpQuestion(missingInformation);

        var clarification = new ClarificationRequest
        {
            ClarificationRequired = true,
            MissingInformation = missingInformation,
            FollowUpQuestion = followUpQuestion,
            ConfidenceScore = analysis is null
                ? 0
                : (analysis.CategoryConfidence + analysis.CountryConfidence + analysis.SpendConfidence) / 3.0
        };

        context.SetMemory(MemoryKeys.ClarificationRequest, clarification);

        return Task.FromResult(new AgentExecutionStepResult
        {
            Success = true,
            Summary = $"Generated follow-up question: \"{followUpQuestion}\"",
            Outputs = new Dictionary<string, object> { [MemoryKeys.ClarificationRequest] = clarification }
        });
    }
}

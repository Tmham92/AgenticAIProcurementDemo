using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Agents;

/// <summary>
/// Wraps <see cref="ICountryRuleService"/> as a plannable agent: retrieves country-specific
/// procurement guidance for the extracted country and stores it in working memory.
/// </summary>
public class CountryGuidanceAgent(ICountryRuleService countryRuleService) : IAgent
{
    private readonly ICountryRuleService _countryRuleService = countryRuleService;

    public string Name => nameof(CountryGuidanceAgent);

    public async Task<AgentExecutionStepResult> ExecuteAsync(AgentRunContext context)
    {
        var analysis = context.GetMemory<ProcurementAnalysis>(MemoryKeys.Analysis)
            ?? throw new InvalidOperationException($"{nameof(CountryGuidanceAgent)} requires {MemoryKeys.Analysis} in working memory.");

        var countryRule = await _countryRuleService.GetCountryRuleAsync(analysis.Country);
        context.SetMemory(MemoryKeys.CountryRule, countryRule!);

        return new AgentExecutionStepResult
        {
            Success = true,
            Summary = countryRule is not null ? $"Guidance Found for {analysis.Country}" : "No Country Guidance Found",
            Outputs = { [MemoryKeys.CountryRule] = countryRule! }
        };
    }
}

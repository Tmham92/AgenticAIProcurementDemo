using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Responsible for analyzing a natural language procurement request using the AI Reasoning
/// Layer (<see cref="ILLMService"/>) and extracting structured data from it.
/// </summary>
public interface IRequestAnalysisService
{
    Task<ProcurementAnalysis> AnalyzeAsync(string message);
}

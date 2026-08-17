namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Generates a single, concise follow-up question asking the user for the specific
/// procurement information that is missing or too uncertain to proceed with.
/// </summary>
public interface IClarificationService
{
    string GenerateFollowUpQuestion(List<string> missingInformation);
}

using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Deterministically generates a single, concise (max 20 words) follow-up question based on
/// which procurement fields are missing or too uncertain to proceed with.
/// </summary>
public class ClarificationService : IClarificationService
{
    public string GenerateFollowUpQuestion(List<string> missingInformation)
    {
        var missingCountry = missingInformation.Any(m => m.Contains("countr", StringComparison.OrdinalIgnoreCase));
        var missingSpend = missingInformation.Any(m => m.Contains("spend", StringComparison.OrdinalIgnoreCase)
            || m.Contains("budget", StringComparison.OrdinalIgnoreCase));
        var missingCategory = missingInformation.Any(m => m.Contains("categor", StringComparison.OrdinalIgnoreCase)
            || m.Contains("product", StringComparison.OrdinalIgnoreCase)
            || m.Contains("service", StringComparison.OrdinalIgnoreCase));

        var missingCount = new[] { missingCountry, missingSpend, missingCategory }.Count(x => x);

        if (missingCount == 0)
        {
            return "Could you provide a bit more detail about your procurement request?";
        }

        if (missingCount > 1)
        {
            if (missingCountry && missingSpend && missingCategory)
            {
                return "Can you describe what you need, which country it's for, and the estimated budget?";
            }

            if (missingCountry && missingSpend)
            {
                return "What country is this for and what is the approximate budget?";
            }

            if (missingCountry && missingCategory)
            {
                return "Can you describe the product or service and which country it's for?";
            }

            return "Can you describe the product or service and its approximate budget?";
        }

        if (missingCountry)
        {
            return "What country will this purchase be made in?";
        }

        if (missingSpend)
        {
            return "What is the estimated budget?";
        }

        return "Can you describe the product or service?";
    }
}

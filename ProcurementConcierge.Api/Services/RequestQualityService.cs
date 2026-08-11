using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Evaluates the completeness of a procurement request across five dimensions
/// (category, country, spend, business justification, supplier information),
/// producing a 0-100 quality score, level, missing information, improvement
/// suggestions, and - when the score is low - an example improved request.
/// </summary>
public class RequestQualityService : IRequestQualityService
{
    private const int PointsPerDimension = RequestQualityHeuristics.PointsPerDimension;

    public RequestQualityAssessment Assess(ProcurementAnalysis analysis, ProcurementPolicy? policy)
    {
        var message = analysis.OriginalMessage ?? string.Empty;

        var hasCategory = RequestQualityHeuristics.HasKnownCategory(analysis.Category);

        var hasCountry = RequestQualityHeuristics.HasKnownCountry(analysis.Country);

        var hasSpend = analysis.EstimatedSpend > 0;

        var hasJustification = RequestQualityHeuristics.HasJustification(message);

        var hasSupplierInformation = HasSupplierInformation(message, policy);

        var missingInformation = new List<string>();
        var suggestions = new List<string>();
        var score = 0;

        if (hasCategory)
        {
            score += PointsPerDimension;
        }
        else
        {
            missingInformation.Add("Category");
            suggestions.Add("Clearly state what product or service you need (e.g. 'marketing agency', 'IT services', 'legal consulting').");
        }

        if (hasCountry)
        {
            score += PointsPerDimension;
        }
        else
        {
            missingInformation.Add("Country");
            suggestions.Add("Specify the country or entity the request applies to, so local guidance can be applied.");
        }

        if (hasSpend)
        {
            score += PointsPerDimension;
        }
        else
        {
            missingInformation.Add("Spend Amount");
            suggestions.Add("Include an estimated budget or spend amount so the correct approval level can be determined.");
        }

        if (hasJustification)
        {
            score += PointsPerDimension;
        }
        else
        {
            missingInformation.Add("Business Justification");
            suggestions.Add("Explain why this purchase is needed (e.g. 'because...', 'in order to...') to support the approval decision.");
        }

        if (hasSupplierInformation)
        {
            score += PointsPerDimension;
        }
        else
        {
            missingInformation.Add("Supplier Information");
            suggestions.Add("Mention whether you intend to use a specific or preferred supplier, or that you need help selecting one.");
        }

        var level = score switch
        {
            >= 80 => "High",
            >= 50 => "Medium",
            _ => "Low"
        };

        var assessment = new RequestQualityAssessment
        {
            RequestQualityScore = score,
            QualityLevel = level,
            MissingInformation = missingInformation,
            ImprovementSuggestions = suggestions
        };

        if (score < 70)
        {
            assessment.ExampleImprovedRequest = BuildExampleImprovedRequest(
                analysis, policy, hasCategory, hasCountry, hasSpend, hasJustification, hasSupplierInformation);
        }

        return assessment;
    }

    private static bool HasSupplierInformation(string message, ProcurementPolicy? policy)
    {
        if (policy is not null && policy.PreferredSuppliers.Any(supplier =>
                message.Contains(supplier, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return RequestQualityHeuristics.HasSupplierKeywordMention(message);
    }

    private static string BuildExampleImprovedRequest(
        ProcurementAnalysis analysis,
        ProcurementPolicy? policy,
        bool hasCategory,
        bool hasCountry,
        bool hasSpend,
        bool hasJustification,
        bool hasSupplierInformation)
    {
        var country = hasCountry ? analysis.Country : "[Country, e.g. France]";
        var category = hasCategory ? analysis.Category : "[Category, e.g. Marketing Services]";
        var spend = hasSpend ? analysis.EstimatedSpend.ToString("C") : "[Estimated budget, e.g. €50,000]";
        var supplier = hasSupplierInformation
            ? "our preferred supplier"
            : policy is { PreferredSuppliers.Count: > 0 }
                ? $"a preferred supplier such as {policy.PreferredSuppliers.First()}"
                : "[preferred supplier name, if any]";
        var justification = hasJustification
            ? "to support an active business need"
            : "[reason, e.g. 'because our current contract has expired']";

        return $"I'm located in {country}. I need {category} {justification}. " +
               $"Estimated budget is {spend}. We would like to use {supplier}.";
    }
}

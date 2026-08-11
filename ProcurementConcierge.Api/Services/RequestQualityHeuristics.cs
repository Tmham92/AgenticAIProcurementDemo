namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Shared heuristics for evaluating procurement request completeness (category, country,
/// spend, business justification, supplier information), used by
/// <see cref="RequestQualityService"/>, <see cref="ProcurementHealthService"/>,
/// <see cref="CountryGovernanceService"/>, <see cref="ProcurementCoachingService"/>, and
/// <see cref="PolicyDeviationService"/> to avoid duplicating scoring logic and keyword lists.
/// </summary>
public static class RequestQualityHeuristics
{
    public const int PointsPerDimension = 20;

    public static readonly string[] JustificationKeywords =
    {
        "because", "since", "in order to", "justification", "so that", "needed to", "required to", "due to"
    };

    public static readonly string[] SupplierKeywords =
    {
        "supplier", "vendor", "agency", "provider", "we use", "we normally use", "our current"
    };

    public static bool HasKnownCategory(string? category) =>
        !string.IsNullOrWhiteSpace(category) && !string.Equals(category, "Unknown", StringComparison.OrdinalIgnoreCase);

    public static bool HasKnownCountry(string? country) =>
        !string.IsNullOrWhiteSpace(country) && !string.Equals(country, "Unknown", StringComparison.OrdinalIgnoreCase);

    public static bool HasJustification(string? message) => ContainsAny(message, JustificationKeywords);

    public static bool HasSupplierKeywordMention(string? message) => ContainsAny(message, SupplierKeywords);

    /// <summary>
    /// Calculates a 0-100 request quality score across five equally weighted dimensions:
    /// known category, known country, spend estimate, business justification, and supplier
    /// information (keyword-based only; does not consider policy preferred suppliers).
    /// </summary>
    public static int CalculateScore(string? category, string? country, decimal estimatedSpend, string? message)
    {
        var score = 0;

        if (HasKnownCategory(category))
        {
            score += PointsPerDimension;
        }

        if (HasKnownCountry(country))
        {
            score += PointsPerDimension;
        }

        if (estimatedSpend > 0)
        {
            score += PointsPerDimension;
        }

        if (HasJustification(message))
        {
            score += PointsPerDimension;
        }

        if (HasSupplierKeywordMention(message))
        {
            score += PointsPerDimension;
        }

        return score;
    }

    private static bool ContainsAny(string? message, string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return keywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}

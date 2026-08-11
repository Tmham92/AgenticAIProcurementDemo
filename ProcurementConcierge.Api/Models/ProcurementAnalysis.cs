namespace ProcurementConcierge.Api.Models;

/// <summary>
/// Structured result extracted from the natural language procurement request
/// by the AI analysis step (Azure OpenAI / Semantic Kernel).
/// </summary>
public class ProcurementAnalysis
{
    public string Category { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal EstimatedSpend { get; set; }

    /// <summary>
    /// Supplier name explicitly mentioned in the request, if any.
    /// </summary>
    public string? SupplierName { get; set; }

    /// <summary>
    /// Business justification text extracted or inferred from the request, if present.
    /// </summary>
    public string? BusinessJustification { get; set; }

    /// <summary>
    /// Original natural language request text, retained for heuristics such as
    /// business-justification detection.
    /// </summary>
    public string OriginalMessage { get; set; } = string.Empty;

    /// <summary>
    /// Confidence (0-100) that the extracted category is correct.
    /// </summary>
    public int CategoryConfidence { get; set; } = 100;

    /// <summary>
    /// Confidence (0-100) that the extracted country is correct.
    /// </summary>
    public int CountryConfidence { get; set; } = 100;

    /// <summary>
    /// Confidence (0-100) that the extracted spend amount is correct.
    /// </summary>
    public int SpendConfidence { get; set; } = 100;

    /// <summary>
    /// True when any extracted field has a confidence below the human-review threshold (70%).
    /// </summary>
    public bool NeedsHumanReview =>
        CategoryConfidence < 70 || CountryConfidence < 70 || SpendConfidence < 70;
}

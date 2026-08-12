using System.Text.Json;
using System.Text.Json.Serialization;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Uses the AI Reasoning Layer (<see cref="ILLMService"/>, backed by a local Ollama model
/// by default) to analyze a natural language procurement request and extract the
/// procurement category, country, estimated spend, supplier name, and business
/// justification, along with per-field confidence scores.
/// </summary>
public partial class RequestAnalysisService(ILLMService llmService, ILogger<RequestAnalysisService> logger) : IRequestAnalysisService
{
    private readonly ILLMService _llmService = llmService;
    private readonly ILogger<RequestAnalysisService> _logger = logger;

    private const string SystemPrompt = """
        You are a procurement intake analyst. Extract structured data from procurement requests.
        Valid categories are: "Marketing Services", "IT Services", "Professional Services".
        If a field is not mentioned, use an empty string (or 0 for numbers, null for optional fields).

        You MUST always include categoryConfidence, countryConfidence, and spendConfidence in your
        JSON response - these are required integer fields and must never be omitted, even when the
        corresponding value was not mentioned in the request. Confidence values must be integers from
        0 to 100 representing how confident you are that the corresponding field was correctly and
        unambiguously extracted from the request. Use a low confidence (below 70) when the field is
        missing, ambiguous, or guessed, and a high confidence (70 or above) only when the value was
        explicitly and unambiguously stated in the request.
        """;

    public async Task<ProcurementAnalysis> AnalyzeAsync(string message)
    {
        try
        {
            var userPrompt =
                $"""
                Extract the following fields from this procurement request as JSON:
                category, country, estimatedSpend, supplierName, businessJustification,
                categoryConfidence, countryConfidence, spendConfidence.

                Request: "{message}"
                """;

            var extraction = await _llmService.GenerateStructuredResponseAsync<ExtractionResult>(SystemPrompt, userPrompt);

            return new ProcurementAnalysis
            {
                Category = string.IsNullOrWhiteSpace(extraction.Category) ? "Unknown" : extraction.Category,
                Country = string.IsNullOrWhiteSpace(extraction.Country) ? "Unknown" : extraction.Country,
                EstimatedSpend = extraction.EstimatedSpend,
                SupplierName = string.IsNullOrWhiteSpace(extraction.SupplierName) ? null : extraction.SupplierName,
                BusinessJustification = string.IsNullOrWhiteSpace(extraction.BusinessJustification) ? null : extraction.BusinessJustification,
                CategoryConfidence = ResolveConfidence(extraction.CategoryConfidence, nameof(extraction.CategoryConfidence)),
                CountryConfidence = ResolveConfidence(extraction.CountryConfidence, nameof(extraction.CountryConfidence)),
                SpendConfidence = ResolveConfidence(extraction.SpendConfidence, nameof(extraction.SpendConfidence)),
                OriginalMessage = message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM analysis failed. Falling back to heuristic extraction.");
        }

        return FallbackAnalyze(message);
    }

    private class ExtractionResult
    {
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;

        [JsonPropertyName("estimatedSpend")]
        public decimal EstimatedSpend { get; set; }

        [JsonPropertyName("supplierName")]
        public string? SupplierName { get; set; }

        [JsonPropertyName("businessJustification")]
        public string? BusinessJustification { get; set; }

        // Nullable so a missing value in the LLM's JSON response can be detected explicitly
        // (rather than silently taking on a C# default), even though the system prompt
        // requires these fields to always be present.
        [JsonPropertyName("categoryConfidence")]
        public int? CategoryConfidence { get; set; }

        [JsonPropertyName("countryConfidence")]
        public int? CountryConfidence { get; set; }

        [JsonPropertyName("spendConfidence")]
        public int? SpendConfidence { get; set; }
    }

    /// <summary>
    /// Resolves a confidence value returned by the LLM. The system prompt requires the LLM to
    /// always include these fields, so a null value here means the model failed to follow
    /// instructions rather than that the field was genuinely low-confidence. Treat that case as
    /// explicitly "needs human review" (0) instead of silently defaulting to a hidden value.
    /// </summary>
    private int ResolveConfidence(int? confidence, string fieldName)
    {
        if (confidence is null)
        {
            _logger.LogWarning(
                "LLM response omitted required confidence field '{FieldName}'. Treating as 0 (needs human review).",
                fieldName);
            return 0;
        }

        return Math.Clamp(confidence.Value, 0, 100);
    }

    /// <summary>
    /// Simple keyword/regex based fallback used when the local LLM is unavailable or
    /// misconfigured, so the PoC remains demonstrable offline.
    /// </summary>
    private static ProcurementAnalysis FallbackAnalyze(string message)
    {
        var lower = message.ToLowerInvariant();

        var category = lower switch
        {
            var m when m.Contains("marketing") => "Marketing Services",
            var m when m.Contains("it ") || m.Contains("software") || m.Contains("technology") => "IT Services",
            var m when m.Contains("consult") || m.Contains("professional") || m.Contains("legal") => "Professional Services",
            _ => "Unknown"
        };

        var country = "Unknown";
        var knownCountries = new[] { "France", "Germany", "Spain", "Italy", "Belgium", "Netherlands", "United States", "United Kingdom" };
        foreach (var c in knownCountries)
        {
            if (lower.Contains(c, StringComparison.InvariantCultureIgnoreCase))
            {
                country = c;
                break;
            }
        }

        decimal estimatedSpend = 0;
        var numberMatch = SpendingRegex().Match(message);
        if (numberMatch.Success)
        {
            estimatedSpend = ParseSpendAmount(numberMatch.Groups[1].Value);
        }

        return new ProcurementAnalysis
        {
            Category = category,
            Country = country,
            EstimatedSpend = estimatedSpend,
            OriginalMessage = message,
            // The regex/keyword fallback is inherently less reliable than an LLM extraction,
            // so confidence is high only when a concrete value was actually matched.
            CategoryConfidence = category == "Unknown" ? 30 : 75,
            CountryConfidence = country == "Unknown" ? 30 : 85,
            SpendConfidence = estimatedSpend > 0 ? 80 : 20
        };
    }

    /// <summary>
    /// Parses a spend amount that may use either US-style (1,000.50) or European-style
    /// (1.000,50) grouping/decimal separators, correctly distinguishing the decimal
    /// separator from thousands separators so re-parsing a previously generated
    /// recommendation (e.g. "€ 90.000,00") does not inflate the amount.
    /// </summary>
    private static decimal ParseSpendAmount(string raw)
    {
        var lastComma = raw.LastIndexOf(',');
        var lastDot = raw.LastIndexOf('.');
        var decimalSeparatorIndex = Math.Max(lastComma, lastDot);

        string cleaned;
        if (decimalSeparatorIndex >= 0 && raw.Length - decimalSeparatorIndex - 1 is 1 or 2)
        {
            // The last separator is followed by 1-2 digits, so treat it as the decimal
            // separator and strip all other grouping separators.
            var integerPart = raw[..decimalSeparatorIndex].Replace(",", string.Empty).Replace(".", string.Empty);
            var fractionalPart = raw[(decimalSeparatorIndex + 1)..];
            cleaned = $"{integerPart}.{fractionalPart}";
        }
        else
        {
            // No clear decimal separator - treat all separators as thousands separators.
            cleaned = raw.Replace(",", string.Empty).Replace(".", string.Empty);
        }

        return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"(\d[\d,\.]*)\s*(euros?|eur|€|\$|usd|dollars?)?", System.Text.RegularExpressions.RegexOptions.IgnoreCase, "nl-NL")]
    private static partial System.Text.RegularExpressions.Regex SpendingRegex();
}


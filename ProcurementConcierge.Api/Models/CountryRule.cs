namespace ProcurementConcierge.Api.Models;

/// <summary>
/// Represents country-specific procurement guidance, as loaded from Data/countryRules.json.
/// </summary>
public class CountryRule
{
    public string Country { get; set; } = string.Empty;
    public string Guidance { get; set; } = string.Empty;
    public bool RequiresLocalLegalReview { get; set; }
    public decimal ApprovalThresholdMultiplier { get; set; } = 1.0m;
}

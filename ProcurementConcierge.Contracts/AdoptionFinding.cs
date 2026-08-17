namespace ProcurementConcierge.Contracts;

/// <summary>
/// A single, structured adoption problem detected by the Adoption Intelligence Agent from
/// historical <c>InteractionRecord</c>s (e.g. repeated policy deviations, frequent supplier
/// exceptions, low-compliance countries/categories, high ProcOps dependency, or common
/// missing information), ranked by <see cref="Severity"/> so leadership can prioritize the
/// most impactful adoption problems first.
/// </summary>
public class AdoptionFinding
{
    /// <summary>The procurement category this finding relates to, or "All" if category-agnostic.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>The country this finding relates to, or "All" if country-agnostic.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>A concise, human-readable description of the adoption problem detected.</summary>
    public string Finding { get; set; } = string.Empty;

    /// <summary>The business impact of this finding (e.g. increased risk, ProcOps overhead, poor adoption).</summary>
    public string Impact { get; set; } = string.Empty;

    /// <summary>A concrete, actionable recommendation to address the finding.</summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// Severity from 1 (minor) to 10 (critical), used to rank findings so the most
    /// impactful adoption problems surface first.
    /// </summary>
    public int Severity { get; set; }
}

namespace ProcurementConcierge.Contracts;

/// <summary>
/// Classifies an executive's Control Tower Chat question so the context builder can
/// prioritize which organizational data sources are most relevant.
/// </summary>
public enum ControlTowerQuestionType
{
    Adoption,
    Compliance,
    Governance,
    ProcOps,
    Executive,
    Country,
    General
}

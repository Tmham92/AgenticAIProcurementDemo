using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Evaluates preferred supplier availability, required approval level, policy deviations,
/// and overall compliance risk for a procurement request.
/// </summary>
public class ComplianceService(ILogger<ComplianceService> logger) : IComplianceService
{
    private readonly ILogger<ComplianceService> _logger = logger;

    public ComplianceEvaluation Evaluate(ProcurementAnalysis analysis, ProcurementPolicy? policy, CountryRule? countryRule)
    {
        var evaluation = new ComplianceEvaluation();

        if (policy is null)
        {
            _logger.LogWarning("No policy found for category '{Category}'.", analysis.Category);
            evaluation.RequiredApproval = "Unknown - no policy found";
            evaluation.ComplianceStatus = "Non-Compliant";
            evaluation.ComplianceRisk = "High";
            evaluation.IsPolicyDeviation = true;
            evaluation.PolicyDeviationReason = $"No global procurement policy exists for category '{analysis.Category}'.";
            return evaluation;
        }

        // Is a preferred supplier available?
        evaluation.PreferredSupplierAvailable = policy.PreferredSuppliers.Count > 0;

        // Apply country-specific approval threshold multiplier, if any.
        var multiplier = countryRule?.ApprovalThresholdMultiplier ?? 1.0m;
        var directorThreshold = policy.DirectorApprovalThreshold * multiplier;
        var cpoThreshold = policy.CpoApprovalThreshold * multiplier;

        // Is director/CPO approval required?
        if (analysis.EstimatedSpend >= cpoThreshold)
        {
            evaluation.RequiredApproval = "Chief Procurement Officer (CPO)";
        }
        else if (analysis.EstimatedSpend >= directorThreshold)
        {
            var director = policy.Category.Replace(" Services", string.Empty) + " Director";
            evaluation.RequiredApproval = director;
        }
        else
        {
            evaluation.RequiredApproval = "Manager";
        }

        // Is there a policy deviation? (e.g. no preferred supplier available, or country requires legal review)
        var deviationReasons = new List<string>();
        if (!evaluation.PreferredSupplierAvailable)
        {
            deviationReasons.Add($"No preferred suppliers are configured for category '{analysis.Category}'.");
        }

        if (countryRule?.RequiresLocalLegalReview == true)
        {
            deviationReasons.Add($"{analysis.Country} requires local legal review before proceeding.");
        }

        evaluation.IsPolicyDeviation = deviationReasons.Count > 0;
        evaluation.PolicyDeviationReason = string.Join(" ", deviationReasons);

        // Determine overall compliance risk.
        evaluation.ComplianceStatus = "Compliant";
        evaluation.ComplianceRisk = evaluation.IsPolicyDeviation
            ? (countryRule?.RequiresLocalLegalReview == true ? "High" : "Medium")
            : "Low";

        return evaluation;
    }
}

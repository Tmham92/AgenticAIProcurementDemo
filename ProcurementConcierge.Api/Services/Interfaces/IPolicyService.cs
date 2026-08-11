using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Responsible for loading and querying procurement policies from local configuration.
/// </summary>
public interface IPolicyService
{
    Task<ProcurementPolicy?> GetPolicyAsync(string category);
    Task<IReadOnlyList<ProcurementPolicy>> GetAllPoliciesAsync();
}

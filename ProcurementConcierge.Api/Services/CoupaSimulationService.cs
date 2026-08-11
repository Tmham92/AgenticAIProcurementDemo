using System.Collections.Concurrent;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;
using ProcurementConcierge.Contracts;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Simulates a future Coupa integration so the Procurement Concierge can demonstrate
/// supplier lookup, requisition drafting, approval routing, and status tracking without
/// requiring a real connection to Coupa. Draft requisitions are held in memory for the
/// lifetime of the process.
/// </summary>
public class CoupaSimulationService(IPolicyService policyService, ILogger<CoupaSimulationService> logger) : ICoupaSimulationService
{
    private readonly IPolicyService _policyService = policyService;
    private readonly ILogger<CoupaSimulationService> _logger = logger;
    private readonly ConcurrentDictionary<string, Requisition> _requisitions = new();

    public async Task<List<Supplier>> GetPreferredSuppliers(string category)
    {
        var policy = await _policyService.GetPolicyAsync(category);

        if (policy is null || policy.PreferredSuppliers.Count == 0)
        {
            return new List<Supplier>();
        }

        return policy.PreferredSuppliers
            .Select((name, index) => new Supplier
            {
                Id = $"SUP-{category.Replace(" ", "").ToUpperInvariant()}-{index + 1}",
                Name = name,
                Category = category,
                IsPreferred = true,
                Rating = index == 0 ? "Gold" : "Silver"
            })
            .ToList();
    }

    public async Task<Requisition> CreateDraftRequisition(CreateRequisitionRequest request)
    {
        var approvalRoute = await GetApprovalRoute(request.Category, request.EstimatedSpend);

        var requisition = new Requisition
        {
            Id = $"REQ-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            Category = request.Category,
            Country = request.Country,
            EstimatedSpend = request.EstimatedSpend,
            SupplierName = request.PreferredSupplierName,
            Status = "Draft",
            ApprovalRoute = approvalRoute.Select(a => a.ApproverRole).ToList()
        };

        _requisitions[requisition.Id] = requisition;

        _logger.LogInformation(
            "Created simulated Coupa draft requisition {RequisitionId} for category {Category} ({EstimatedSpend:C}).",
            requisition.Id, requisition.Category, requisition.EstimatedSpend);

        return requisition;
    }

    public async Task<List<Approval>> GetApprovalRoute(string category, decimal amount)
    {
        var policy = await _policyService.GetPolicyAsync(category);
        var route = new List<Approval>
        {
            new() { Step = 1, ApproverRole = "Requester Manager", Status = "Pending" }
        };

        if (policy is null)
        {
            route.Add(new Approval { Step = route.Count + 1, ApproverRole = "Procurement Operations (Unmapped Category)", Status = "Pending" });
            return route;
        }

        if (amount >= policy.CpoApprovalThreshold)
        {
            route.Add(new Approval { Step = route.Count + 1, ApproverRole = policy.Category.Replace(" Services", string.Empty) + " Director", Status = "Pending" });
            route.Add(new Approval { Step = route.Count + 1, ApproverRole = "Chief Procurement Officer (CPO)", Status = "Pending" });
        }
        else if (amount >= policy.DirectorApprovalThreshold)
        {
            route.Add(new Approval { Step = route.Count + 1, ApproverRole = policy.Category.Replace(" Services", string.Empty) + " Director", Status = "Pending" });
        }

        return route;
    }

    public Requisition? GetRequisitionStatus(string id)
    {
        return _requisitions.TryGetValue(id, out var requisition) ? requisition : null;
    }
}

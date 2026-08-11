using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Persists a history of processed procurement requests for use by adoption insight,
/// process discovery, and executive insight services.
/// </summary>
public interface IRequestHistoryStore
{
    void Record(RequestHistoryEntry entry);
    IReadOnlyList<RequestHistoryEntry> GetAll();
}

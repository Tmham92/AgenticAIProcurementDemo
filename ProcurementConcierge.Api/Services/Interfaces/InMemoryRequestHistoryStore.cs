using System.Collections.Concurrent;
using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Services.Interfaces;

/// <summary>
/// Simple in-memory, thread-safe request history store. Suitable for a proof-of-concept;
/// can be swapped for a database-backed implementation later behind <see cref="IRequestHistoryStore"/>.
/// </summary>
public class InMemoryRequestHistoryStore : IRequestHistoryStore
{
    private readonly ConcurrentQueue<RequestHistoryEntry> _entries = new();

    public void Record(RequestHistoryEntry entry)
    {
        _entries.Enqueue(entry);
    }

    public IReadOnlyList<RequestHistoryEntry> GetAll()
    {
        return _entries.ToList();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ProcurementConcierge.Api.Data;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Persists <see cref="InteractionRecord"/> entries to the local SQLite database using
/// EF Core, so insights from user interactions can be collected and reported on over time.
/// </summary>
public class InteractionLoggingService(InteractionDbContext dbContext, IMemoryCache cache, ILogger<InteractionLoggingService> logger) : IInteractionLoggingService
{
    private const string AllInteractionsCacheKey = "InteractionLoggingService:AllInteractions";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    // Guards against a cache stampede: several dashboard requests can miss the cache at the
    // same time (e.g. Task.WhenAll fan-out), so without this lock they'd each independently
    // hit the database instead of sharing a single query result.
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    private readonly InteractionDbContext _dbContext = dbContext;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<InteractionLoggingService> _logger = logger;

    public async Task LogInteractionAsync(InteractionRecord record)
    {
        _dbContext.Interactions.Add(record);
        await _dbContext.SaveChangesAsync();
        _cache.Remove(AllInteractionsCacheKey);
        _logger.LogInformation("Logged interaction {InteractionId} for category {Category}.", record.Id, record.Category);
    }

    public async Task<List<InteractionRecord>> GetAllAsync()
    {
        // Dashboard-style pages fan out several requests that each independently need the
        // full interaction history. Cache it briefly so those concurrent/near-concurrent
        // calls share a single DB round-trip instead of each re-scanning the table.
        if (_cache.TryGetValue(AllInteractionsCacheKey, out List<InteractionRecord>? cached) && cached is not null)
        {
            return cached;
        }

        await CacheLock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(AllInteractionsCacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            var records = await _dbContext.Interactions
                .AsNoTracking()
                .OrderByDescending(i => i.Timestamp)
                .ToListAsync();

            _cache.Set(AllInteractionsCacheKey, records, CacheDuration);
            return records;
        }
        finally
        {
            CacheLock.Release();
        }
    }
}

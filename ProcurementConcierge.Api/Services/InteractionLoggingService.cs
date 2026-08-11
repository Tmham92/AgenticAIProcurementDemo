using Microsoft.EntityFrameworkCore;
using ProcurementConcierge.Api.Data;
using ProcurementConcierge.Api.Models;
using ProcurementConcierge.Api.Services.Interfaces;

namespace ProcurementConcierge.Api.Services;

/// <summary>
/// Persists <see cref="InteractionRecord"/> entries to the local SQLite database using
/// EF Core, so insights from user interactions can be collected and reported on over time.
/// </summary>
public class InteractionLoggingService(InteractionDbContext dbContext, ILogger<InteractionLoggingService> logger) : IInteractionLoggingService
{
    private readonly InteractionDbContext _dbContext = dbContext;
    private readonly ILogger<InteractionLoggingService> _logger = logger;

    public async Task LogInteractionAsync(InteractionRecord record)
    {
        _dbContext.Interactions.Add(record);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Logged interaction {InteractionId} for category {Category}.", record.Id, record.Category);
    }

    public async Task<List<InteractionRecord>> GetAllAsync()
    {
        return await _dbContext.Interactions
            .AsNoTracking()
            .OrderByDescending(i => i.Timestamp)
            .ToListAsync();
    }
}

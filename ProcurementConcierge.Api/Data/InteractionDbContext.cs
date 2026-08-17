using Microsoft.EntityFrameworkCore;
using ProcurementConcierge.Api.Models;

namespace ProcurementConcierge.Api.Data;

/// <summary>
/// EF Core database context backing the local SQLite interaction log, used to persist
/// <see cref="InteractionRecord"/> entries for every analyzed procurement request.
/// </summary>
public class InteractionDbContext(DbContextOptions<InteractionDbContext> options) : DbContext(options)
{
    public DbSet<InteractionRecord> Interactions => Set<InteractionRecord>();

    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();

    public DbSet<ControlTowerConversation> ControlTowerConversations => Set<ControlTowerConversation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InteractionRecord>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.OriginalRequest).IsRequired();
            entity.Property(i => i.Category).IsRequired();
            entity.Property(i => i.Country).IsRequired();
            entity.Property(i => i.ComplianceLevel).IsRequired();
            entity.Property(i => i.Recommendation).IsRequired();
            entity.Property(i => i.DeviationTypes)
                .HasConversion(
                    types => string.Join('|', types),
                    value => value.Length == 0
                        ? new List<string>()
                        : value.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Title).IsRequired();
            entity.Property(d => d.Category).IsRequired();
            entity.Property(d => d.Country).IsRequired();
            entity.Property(d => d.Content).IsRequired();
            entity.Property(d => d.Source).IsRequired();
        });

        modelBuilder.Entity<ControlTowerConversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Question).IsRequired();
            entity.Property(c => c.Answer).IsRequired();
        });
    }
}

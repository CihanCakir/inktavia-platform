using Aizen.Core.EFCore;
using Aizen.Modules.Profile.Domain.Entities.Performance;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Profile.Repository.Persistence;

[DocumentationInfo("Profile EF DbContext",
    "EF Core context for the Profile module PostgreSQL schema (profile.*). " +
    "Hosts the Profile.Performance sub-module tables.")]
public sealed class ProfileDbContext : AizenDbContext
{
    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options) { }

    // ── Performance sub-module ────────────────────────────────────────────────
    public DbSet<ProfilePerformanceSnapshotEntity> PerformanceSnapshots => Set<ProfilePerformanceSnapshotEntity>();
    public DbSet<ProfileScoreComponentEntity>      ScoreComponents      => Set<ProfileScoreComponentEntity>();
    public DbSet<ProfileScoreHistoryEntity>        ScoreHistory         => Set<ProfileScoreHistoryEntity>();
    public DbSet<ProfileDecisionLogEntity>         DecisionLogs         => Set<ProfileDecisionLogEntity>();
    public DbSet<ProfileRiskSignalEntity>          RiskSignals          => Set<ProfileRiskSignalEntity>();
    public DbSet<ProfileMetricCacheEntity>         MetricCaches         => Set<ProfileMetricCacheEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("profile");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfileDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimeProperties();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        NormalizeDateTimeProperties();
        return base.SaveChanges();
    }

    private void NormalizeDateTimeProperties()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Local)
                    property.CurrentValue = dt.ToUniversalTime();
            }
        }
    }
}

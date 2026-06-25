using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Persistence;

public sealed class CargoDryDbContext : DbContext
{
    public CargoDryDbContext(DbContextOptions<CargoDryDbContext> options) : base(options) { }

    public DbSet<CargoDryProductEntity>       Products       => Set<CargoDryProductEntity>();
    public DbSet<CargoDryBatchEntity>         Batches        => Set<CargoDryBatchEntity>();
    public DbSet<CargoDryKitEntity>           Kits           => Set<CargoDryKitEntity>();
    public DbSet<CargoDryActivationLogEntity> ActivationLogs => Set<CargoDryActivationLogEntity>();
    public DbSet<CargoDryRenewalEntity>       Renewals       => Set<CargoDryRenewalEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cargodry");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CargoDryDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

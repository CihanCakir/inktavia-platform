using Aizen.Core.EFCore;
using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.CargoDry.Repository.Persistence;

[DocumentationInfo("CargoDry EF DbContext",
    "EF Core context for the CargoDry module PostgreSQL schema.")]
public sealed class CargoDryDbContext : AizenDbContext
{
    public CargoDryDbContext(DbContextOptions<CargoDryDbContext> options) : base(options) { }

    public DbSet<CargoDryProductEntity>  Products  => Set<CargoDryProductEntity>();
    public DbSet<CargoDryBatchEntity>    Batches   => Set<CargoDryBatchEntity>();
    public DbSet<CargoDryKitEntity>      Kits      => Set<CargoDryKitEntity>();
    public DbSet<CargoDryRenewalEntity>  Renewals  => Set<CargoDryRenewalEntity>();
    // ActivationLogs — moved to MongoDB (CargoDryMongoDbContext)

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("cargodry");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CargoDryDbContext).Assembly);
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

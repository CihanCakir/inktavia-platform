using Aizen.Core.EFCore;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Repository.Persistence;

[DocumentationInfo("Vessel EF DbContext", "EF Core context for the vessel PostgreSQL schema.")]
public sealed class VesselDbContext : AizenDbContext
{
    public VesselDbContext(DbContextOptions<VesselDbContext> options)
        : base(options)
    {
    }

    public DbSet<VesselEntity> Vessels => Set<VesselEntity>();
    public DbSet<VesselOwnerEntity> VesselOwners => Set<VesselOwnerEntity>();
    public DbSet<VesselSpecificationEntity> VesselSpecifications => Set<VesselSpecificationEntity>();
    public DbSet<VesselEngineEntity> VesselEngines => Set<VesselEngineEntity>();
    public DbSet<VesselDocumentEntity> VesselDocuments => Set<VesselDocumentEntity>();
    public DbSet<VesselMediaEntity> VesselMedia => Set<VesselMediaEntity>();
    public DbSet<VesselLocationSnapshotEntity> VesselLocationSnapshots => Set<VesselLocationSnapshotEntity>();
    public DbSet<VesselStatusHistoryEntity> VesselStatusHistories => Set<VesselStatusHistoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("vessel");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VesselDbContext).Assembly);
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
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Local)
                    property.CurrentValue = dt.ToUniversalTime();
            }
        }
    }
}

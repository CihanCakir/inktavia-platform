using Aizen.Modules.ServiceRequest.Domain.Entities.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

[DocumentationInfo("Maintenance schedule EF config", "Table + indexes for MaintenanceScheduleEntity (S12/N2).")]
public sealed class MaintenanceScheduleEntityConfiguration : IEntityTypeConfiguration<MaintenanceScheduleEntity>
{
    public void Configure(EntityTypeBuilder<MaintenanceScheduleEntity> builder)
    {
        builder.ToTable("maintenance_schedules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceCategoryCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ServiceTypeCode).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        // One active schedule per (vessel, category, type). Partial unique so deactivated/deleted rows don't collide.
        builder.HasIndex(x => new { x.VesselId, x.ServiceCategoryCode, x.ServiceTypeCode })
            .IsUnique()
            .HasFilter("\"IsActive\" = true AND \"IsDeleted\" = false");

        // N2 job scan support.
        builder.HasIndex(x => x.NextDueAt);
        builder.HasIndex(x => x.OwnerUserId);
    }
}

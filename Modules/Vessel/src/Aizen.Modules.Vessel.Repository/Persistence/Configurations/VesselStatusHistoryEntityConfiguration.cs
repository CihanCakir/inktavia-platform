using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Vessel.Repository.Persistence.Configurations;

public sealed class VesselStatusHistoryEntityConfiguration : IEntityTypeConfiguration<VesselStatusHistoryEntity>
{
    public void Configure(EntityTypeBuilder<VesselStatusHistoryEntity> builder)
    {
        builder.ToTable("vessel_status_histories", "vessel");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VesselId).IsRequired();
        builder.Property(x => x.FromStatus);
        builder.Property(x => x.ToStatus).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.ChangedByUserId);
        builder.Property(x => x.ChangedAt).IsRequired();

        builder.HasIndex(x => new { x.VesselId, x.ChangedAt });

        builder.HasOne(x => x.Vessel)
            .WithMany(x => x.StatusHistory)
            .HasForeignKey(x => x.VesselId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Vessel.Repository.Persistence.Configurations;

public sealed class VesselOwnerEntityConfiguration : IEntityTypeConfiguration<VesselOwnerEntity>
{
    public void Configure(EntityTypeBuilder<VesselOwnerEntity> builder)
    {
        builder.ToTable("vessel_owners", "vessel");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VesselId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Role).IsRequired();
        builder.Property(x => x.OwnershipStatus).IsRequired();
        builder.Property(x => x.IsPrimary).IsRequired();
        builder.Property(x => x.InvitedAt);
        builder.Property(x => x.AcceptedAt);
        builder.Property(x => x.RemovedAt);

        builder.HasIndex(x => new { x.VesselId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.VesselId, x.IsPrimary });

        builder.HasOne(x => x.Vessel)
            .WithMany(x => x.Owners)
            .HasForeignKey(x => x.VesselId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

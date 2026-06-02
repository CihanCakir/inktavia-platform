using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Vessel.Repository.Persistence.Configurations;

public sealed class VesselMediaEntityConfiguration : IEntityTypeConfiguration<VesselMediaEntity>
{
    public void Configure(EntityTypeBuilder<VesselMediaEntity> builder)
    {
        builder.ToTable("vessel_media", "vessel");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VesselId).IsRequired();
        builder.Property(x => x.MediaType).IsRequired();
        builder.Property(x => x.FileId).HasMaxLength(500);
        builder.Property(x => x.FileName).HasMaxLength(500);
        builder.Property(x => x.FileUrl).HasMaxLength(2000);
        builder.Property(x => x.IsCover).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();

        builder.HasIndex(x => new { x.VesselId, x.IsCover });
        builder.HasIndex(x => new { x.VesselId, x.MediaType });

        builder.HasOne(x => x.Vessel)
            .WithMany(x => x.Media)
            .HasForeignKey(x => x.VesselId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Vessel.Repository.Persistence.Configurations;

public sealed class VesselLocationSnapshotEntityConfiguration : IEntityTypeConfiguration<VesselLocationSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<VesselLocationSnapshotEntity> builder)
    {
        builder.ToTable("vessel_location_snapshots", "vessel");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VesselId).IsRequired();
        builder.Property(x => x.Latitude).HasPrecision(10, 7);
        builder.Property(x => x.Longitude).HasPrecision(10, 7);
        builder.Property(x => x.AccuracyMeters).HasPrecision(10, 3);
        builder.Property(x => x.CountryCode).HasMaxLength(10);
        builder.Property(x => x.CityCode).HasMaxLength(100);
        builder.Property(x => x.DistrictCode).HasMaxLength(100);
        builder.Property(x => x.MarinaName).HasMaxLength(200);
        builder.Property(x => x.Source).HasMaxLength(100);
        builder.Property(x => x.IsCurrent).IsRequired();
        builder.Property(x => x.CapturedAt).IsRequired();

        builder.HasIndex(x => new { x.VesselId, x.IsCurrent });
        builder.HasIndex(x => new { x.VesselId, x.CapturedAt });

        builder.HasOne(x => x.Vessel)
            .WithMany(x => x.LocationSnapshots)
            .HasForeignKey(x => x.VesselId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

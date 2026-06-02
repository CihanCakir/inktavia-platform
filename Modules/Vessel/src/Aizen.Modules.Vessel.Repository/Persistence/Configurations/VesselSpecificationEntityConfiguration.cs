using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Vessel.Repository.Persistence.Configurations;

public sealed class VesselSpecificationEntityConfiguration : IEntityTypeConfiguration<VesselSpecificationEntity>
{
    public void Configure(EntityTypeBuilder<VesselSpecificationEntity> builder)
    {
        builder.ToTable("vessel_specifications", "vessel");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VesselId).IsRequired();
        builder.Property(x => x.Brand).HasMaxLength(200);
        builder.Property(x => x.Model).HasMaxLength(200);
        builder.Property(x => x.ProductionYear);
        builder.Property(x => x.LengthValue).HasPrecision(10, 3);
        builder.Property(x => x.LengthUnitCode).HasMaxLength(50);
        builder.Property(x => x.BeamValue).HasPrecision(10, 3);
        builder.Property(x => x.BeamUnitCode).HasMaxLength(50);
        builder.Property(x => x.DraftValue).HasPrecision(10, 3);
        builder.Property(x => x.DraftUnitCode).HasMaxLength(50);
        builder.Property(x => x.WeightValue).HasPrecision(14, 3);
        builder.Property(x => x.WeightUnitCode).HasMaxLength(50);
        builder.Property(x => x.CabinCount);
        builder.Property(x => x.BedCount);
        builder.Property(x => x.BathroomCount);
        builder.Property(x => x.HullMaterialCode).HasMaxLength(100);
        builder.Property(x => x.FuelCapacityValue).HasPrecision(14, 2);
        builder.Property(x => x.FuelCapacityUnitCode).HasMaxLength(50);
        builder.Property(x => x.WaterCapacityValue).HasPrecision(14, 2);
        builder.Property(x => x.WaterCapacityUnitCode).HasMaxLength(50);

        builder.HasIndex(x => x.VesselId).IsUnique();

        builder.HasOne(x => x.Vessel)
            .WithOne(x => x.Specification)
            .HasForeignKey<VesselSpecificationEntity>(x => x.VesselId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

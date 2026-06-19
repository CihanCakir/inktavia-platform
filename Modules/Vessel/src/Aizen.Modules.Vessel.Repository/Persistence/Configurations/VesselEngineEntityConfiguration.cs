using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Vessel.Repository.Persistence.Configurations;

public sealed class VesselEngineEntityConfiguration : IEntityTypeConfiguration<VesselEngineEntity>
{
    public void Configure(EntityTypeBuilder<VesselEngineEntity> builder)
    {
        builder.ToTable("vessel_engines", "vessel");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VesselId).IsRequired();
        builder.Property(x => x.EngineName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EngineTypeCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FuelTypeCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Brand).HasMaxLength(200);
        builder.Property(x => x.Model).HasMaxLength(200);
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.HorsePower);
        builder.Property(x => x.ProductionYear);
        builder.Property(x => x.IsPrimary).IsRequired();
        builder.Property(x => x.PropulsionType).HasMaxLength(100);
        builder.Property(x => x.EnginePowerKw).HasPrecision(14, 2);
        builder.Property(x => x.FuelCapacityL).HasPrecision(14, 2);
        builder.Property(x => x.MaxSpeedKnots).HasPrecision(10, 2);
        builder.Property(x => x.CruisingSpeedKnots).HasPrecision(10, 2);
        builder.Property(x => x.RangeNm).HasPrecision(14, 2);

        builder.HasIndex(x => new { x.VesselId, x.SerialNumber });
        builder.HasIndex(x => new { x.VesselId, x.IsPrimary });

        builder.HasOne(x => x.Vessel)
            .WithMany(x => x.Engines)
            .HasForeignKey(x => x.VesselId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

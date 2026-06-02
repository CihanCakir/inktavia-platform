using Aizen.Modules.ReferenceData.Domain.Entities.Measurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ReferenceData.Repository.Configurations;

public sealed class MeasurementUnitEntityConfiguration : IEntityTypeConfiguration<MeasurementUnitEntity>
{
    public void Configure(EntityTypeBuilder<MeasurementUnitEntity> builder)
    {
        builder.ToTable("measurement_units", "ref");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Symbol).HasMaxLength(20).IsRequired();
        builder.Property(x => x.UnitType).IsRequired();
        builder.Property(x => x.ConversionFactorToBase).HasPrecision(18, 8);
        builder.Property(x => x.BaseUnitCode).HasMaxLength(100);
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.UnitType);
    }
}

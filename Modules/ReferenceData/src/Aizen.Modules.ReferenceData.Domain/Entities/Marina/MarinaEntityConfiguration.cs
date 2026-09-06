using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ReferenceData.Domain.Entities.Marina;

public sealed class MarinaEntityConfiguration : IEntityTypeConfiguration<MarinaEntity>
{
    public void Configure(EntityTypeBuilder<MarinaEntity> builder)
    {
        builder.ToTable("marinas", "ref");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CountryCode).HasMaxLength(10);
        builder.Property(x => x.CityCode).HasMaxLength(100);
        builder.Property(x => x.Province).HasMaxLength(100);
        builder.Property(x => x.District).HasMaxLength(100);
        builder.Property(x => x.Latitude).HasPrecision(10, 7).IsRequired();
        builder.Property(x => x.Longitude).HasPrecision(10, 7).IsRequired();
        builder.Property(x => x.OsmId).HasMaxLength(30);
        builder.Property(x => x.NeedsReview).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.CountryCode);
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => new { x.Latitude, x.Longitude });
    }
}

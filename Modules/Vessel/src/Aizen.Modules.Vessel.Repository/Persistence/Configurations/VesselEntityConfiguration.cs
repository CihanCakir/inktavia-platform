using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Vessel.Repository.Persistence.Configurations;

public sealed class VesselEntityConfiguration : IEntityTypeConfiguration<VesselEntity>
{
    public void Configure(EntityTypeBuilder<VesselEntity> builder)
    {
        builder.ToTable("vessels", "vessel");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VesselCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(250).IsRequired();
        builder.Property(x => x.VesselTypeCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.VesselUsageTypeCode).HasMaxLength(100);
        builder.Property(x => x.FlagCountryCode).HasMaxLength(10);
        builder.Property(x => x.RegistrationNumber).HasMaxLength(100);
        builder.Property(x => x.MmsiNumber).HasMaxLength(20);
        builder.Property(x => x.ImoNumber).HasMaxLength(20);
        builder.Property(x => x.CallSign).HasMaxLength(20);
        builder.Property(x => x.HomeCountryCode).HasMaxLength(10);
        builder.Property(x => x.HomeCityCode).HasMaxLength(100);
        builder.Property(x => x.HomeDistrictCode).HasMaxLength(100);
        builder.Property(x => x.HomeMarinaName).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Visibility).IsRequired();
        builder.Property(x => x.IsArchived).IsRequired();
        builder.Property(x => x.ArchiveReason);
        builder.Property(x => x.ArchivedAt);
        builder.Property(x => x.OperationalStatus);
        builder.Property(x => x.AssetType);

        builder.HasIndex(x => x.VesselCode).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.RegistrationNumber);
        builder.HasIndex(x => x.MmsiNumber);
        builder.HasIndex(x => x.ImoNumber);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.OperationalStatus);
        builder.HasIndex(x => x.AssetType);
    }
}

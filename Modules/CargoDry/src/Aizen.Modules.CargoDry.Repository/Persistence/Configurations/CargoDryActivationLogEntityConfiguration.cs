using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryActivationLogEntityConfiguration : IEntityTypeConfiguration<CargoDryActivationLogEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryActivationLogEntity> builder)
    {
        builder.ToTable("activation_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.KitId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.VesselId).IsRequired();
        builder.Property(x => x.ActivatedAt).IsRequired();
        builder.Property(x => x.Method).IsRequired();
        builder.Property(x => x.Source).IsRequired();
        builder.Property(x => x.DeviceInfo).HasMaxLength(500);
        builder.Property(x => x.IpAddress).HasMaxLength(50);
        builder.HasIndex(x => x.KitId);
        builder.HasIndex(x => x.UserId);
    }
}

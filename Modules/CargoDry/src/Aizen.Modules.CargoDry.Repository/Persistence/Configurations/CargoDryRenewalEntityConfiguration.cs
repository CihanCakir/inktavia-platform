using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryRenewalEntityConfiguration : IEntityTypeConfiguration<CargoDryRenewalEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryRenewalEntity> builder)
    {
        builder.ToTable("renewals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.KitId).IsRequired();
        builder.Property(x => x.OwnerUserId).IsRequired();
        builder.Property(x => x.RenewedAt).IsRequired();
        builder.Property(x => x.NewExpiresAt).IsRequired();
        builder.Property(x => x.AddedDays).IsRequired();
        builder.Property(x => x.RenewalType).IsRequired();
        builder.Property(x => x.PaymentRef).HasMaxLength(200);
        builder.Property(x => x.AdminUserId);
        builder.HasIndex(x => x.KitId);
    }
}

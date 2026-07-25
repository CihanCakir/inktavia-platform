using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class ProviderPaymentProfileConfiguration : IEntityTypeConfiguration<ProviderPaymentProfileEntity>
{
    public void Configure(EntityTypeBuilder<ProviderPaymentProfileEntity> b)
    {
        b.ToTable("provider_payment_profiles");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ProviderProfileId).IsUnique();
        b.Property(x => x.GatewayProvider).HasMaxLength(50).IsRequired();
        b.Property(x => x.SubMerchantKey).HasMaxLength(200);
        b.Property(x => x.SubMerchantAccountId).HasMaxLength(200);
        b.Property(x => x.IbanEncrypted).HasMaxLength(500);  // Encrypted at rest
        b.Property(x => x.IbanLast4).HasMaxLength(4);
        b.Property(x => x.LegalName).HasMaxLength(300);
        b.Property(x => x.TaxNumber).HasMaxLength(50);
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
    }
}

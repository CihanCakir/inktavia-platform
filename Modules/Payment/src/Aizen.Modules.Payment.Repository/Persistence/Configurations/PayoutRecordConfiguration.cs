using Aizen.Modules.Payment.Domain.Entities.Payout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class PayoutRecordConfiguration : IEntityTypeConfiguration<PayoutRecordEntity>
{
    public void Configure(EntityTypeBuilder<PayoutRecordEntity> b)
    {
        b.ToTable("payout_records");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProviderProfileId).IsRequired();
        b.Property(x => x.PaymentTransactionId).IsRequired();
        b.Property(x => x.Amount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.GatewayProvider).HasMaxLength(50).IsRequired();
        b.Property(x => x.GatewayPayoutId).HasMaxLength(200);
        b.Property(x => x.RequestedAt).IsRequired();
        b.Property(x => x.FailureReason).HasMaxLength(500);
        b.Property(x => x.AdminNote).HasMaxLength(500);
        b.HasIndex(x => x.ProviderProfileId);
        b.HasIndex(x => x.Status);
    }
}

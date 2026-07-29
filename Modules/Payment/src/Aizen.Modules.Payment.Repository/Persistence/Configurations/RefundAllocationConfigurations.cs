using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

public sealed class RefundAllocationPolicyConfiguration : IEntityTypeConfiguration<RefundAllocationPolicyEntity>
{
    public void Configure(EntityTypeBuilder<RefundAllocationPolicyEntity> b)
    {
        b.ToTable("refund_allocation_policies");
        b.HasKey(x => x.Id);
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.NegativeBalanceLimit).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.PolicyCode).HasMaxLength(30);
        b.Property(x => x.PolicyName).HasMaxLength(200);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.EffectiveFrom).IsRequired();
        b.HasIndex(x => x.PolicyCode).IsUnique().HasFilter("\"PolicyCode\" IS NOT NULL");
        b.HasIndex(x => new { x.CurrencyCode, x.Status, x.EffectiveFrom, x.EffectiveTo });
        b.HasMany(x => x.Rules).WithOne().HasForeignKey(r => r.RefundAllocationPolicyId).OnDelete(DeleteBehavior.Cascade);
        b.Metadata.FindNavigation(nameof(RefundAllocationPolicyEntity.Rules))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class RefundAllocationPolicyRuleConfiguration : IEntityTypeConfiguration<RefundAllocationPolicyRuleEntity>
{
    public void Configure(EntityTypeBuilder<RefundAllocationPolicyRuleEntity> b)
    {
        b.ToTable("refund_allocation_policy_rules");
        b.HasKey(x => x.Id);
        b.Property(x => x.RefundAllocationPolicyId).IsRequired();
        b.Property(x => x.Cause).HasConversion<int>().IsRequired();
        b.Property(x => x.PlatformFeeRefundMode).HasConversion<int>().IsRequired();
        b.Property(x => x.FixedPlatformFeeAmount).HasColumnType("numeric(18,4)");
        b.HasIndex(x => new { x.RefundAllocationPolicyId, x.Cause }).IsUnique();
    }
}

public sealed class RefundAllocationEntityConfiguration : IEntityTypeConfiguration<RefundAllocationEntity>
{
    public void Configure(EntityTypeBuilder<RefundAllocationEntity> b)
    {
        b.ToTable("refund_allocations");
        b.HasKey(x => x.Id);
        b.Property(x => x.RefundRecordId).IsRequired();
        b.Property(x => x.EconomicsSnapshotId).IsRequired();
        b.Property(x => x.Cause).HasConversion<int>().IsRequired();
        b.Property(x => x.ReleaseState).HasConversion<int>().IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        foreach (var col in new[]
        {
            nameof(RefundAllocationEntity.ServiceRefundAmount), nameof(RefundAllocationEntity.ProviderNetReversalAmount),
            nameof(RefundAllocationEntity.CommissionRevenueReversalAmount), nameof(RefundAllocationEntity.PlatformFeeNetRefundAmount),
            nameof(RefundAllocationEntity.PlatformFeeVatRefundAmount), nameof(RefundAllocationEntity.PlatformFeeGrossRefundAmount),
            nameof(RefundAllocationEntity.GatewayRefundExpenseAmount), nameof(RefundAllocationEntity.ProviderRecoveryAmount),
            nameof(RefundAllocationEntity.PlatformAdvancedRefundAmount), nameof(RefundAllocationEntity.RemainingProviderNegativeBalance),
        })
            b.Property(col).HasColumnType("numeric(18,4)").IsRequired();
        b.HasIndex(x => x.RefundRecordId).IsUnique();
        b.HasIndex(x => x.EconomicsSnapshotId);
    }
}

public sealed class ProviderBalanceConfiguration : IEntityTypeConfiguration<ProviderBalanceEntity>
{
    public void Configure(EntityTypeBuilder<ProviderBalanceEntity> b)
    {
        b.ToTable("provider_balances");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProviderProfileId).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.Balance).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.NegativeBalanceLimit).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.ProviderProfileId, x.CurrencyCode }).IsUnique();
        b.HasMany(x => x.Movements).WithOne().HasForeignKey(m => m.ProviderBalanceId).OnDelete(DeleteBehavior.Cascade);
        b.Metadata.FindNavigation(nameof(ProviderBalanceEntity.Movements))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProviderBalanceMovementConfiguration : IEntityTypeConfiguration<ProviderBalanceMovementEntity>
{
    public void Configure(EntityTypeBuilder<ProviderBalanceMovementEntity> b)
    {
        b.ToTable("provider_balance_movements");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProviderBalanceId).IsRequired();
        b.Property(x => x.MovementType).HasConversion<int>().IsRequired();
        b.Property(x => x.Amount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.BalanceAfter).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.OccurredAtUtc).IsRequired();
        b.HasIndex(x => x.ProviderBalanceId);
    }
}

public sealed class ChargebackRecordConfiguration : IEntityTypeConfiguration<ChargebackRecordEntity>
{
    public void Configure(EntityTypeBuilder<ChargebackRecordEntity> b)
    {
        b.ToTable("chargeback_records");
        b.HasKey(x => x.Id);
        b.Property(x => x.PaymentTransactionId).IsRequired();
        b.Property(x => x.GatewayChargebackReference).HasMaxLength(200).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.Amount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ChargebackExpenseAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.ProviderRecoveredAmount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.RemainingNegativeBalance).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.ReceivedAtUtc).IsRequired();
        b.HasIndex(x => x.GatewayChargebackReference).IsUnique();   // §21.2 idempotency
        b.HasIndex(x => x.PaymentTransactionId);
    }
}

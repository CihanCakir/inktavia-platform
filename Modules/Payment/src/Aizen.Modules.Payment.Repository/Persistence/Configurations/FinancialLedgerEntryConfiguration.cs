using Aizen.Modules.Payment.Domain.Entities.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Payment.Repository.Persistence.Configurations;

/// <summary>
/// BE-P12 §15/§19.18 — append-only financial ledger. Money numeric(18,4); enums int; unique EntryCode; idempotency unique
/// <c>(SourceType, SourceRef, AccountLine, IsReversal)</c>; reporting indexes on (AccountLine, OccurredAtUtc),
/// (ProviderProfileId, OccurredAtUtc) and CurrencyCode.
/// </summary>
public sealed class FinancialLedgerEntryConfiguration : IEntityTypeConfiguration<FinancialLedgerEntryEntity>
{
    public void Configure(EntityTypeBuilder<FinancialLedgerEntryEntity> b)
    {
        b.ToTable("financial_ledger_entries");
        b.HasKey(x => x.Id);

        b.Property(x => x.EntryCode).HasMaxLength(40).IsRequired();
        b.Property(x => x.AccountLine).HasConversion<int>().IsRequired();
        b.Property(x => x.Nature).HasConversion<int>().IsRequired();
        b.Property(x => x.Amount).HasColumnType("numeric(18,4)").IsRequired();
        b.Property(x => x.IsReversal).IsRequired().HasDefaultValue(false);
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.SourceType).HasConversion<int>().IsRequired();
        b.Property(x => x.SourceRef).IsRequired();
        b.Property(x => x.TransactionId);
        b.Property(x => x.ProviderProfileId);
        b.Property(x => x.CustomerProfileId);
        b.Property(x => x.OccurredAtUtc).IsRequired();
        b.Property(x => x.PostedAtUtc).IsRequired();
        b.Property(x => x.Note).HasMaxLength(500);

        b.HasIndex(x => x.EntryCode).IsUnique();
        b.HasIndex(x => new { x.SourceType, x.SourceRef, x.AccountLine, x.IsReversal }).IsUnique();   // idempotency
        b.HasIndex(x => new { x.AccountLine, x.OccurredAtUtc });
        b.HasIndex(x => new { x.ProviderProfileId, x.OccurredAtUtc });
        b.HasIndex(x => x.CurrencyCode);
    }
}

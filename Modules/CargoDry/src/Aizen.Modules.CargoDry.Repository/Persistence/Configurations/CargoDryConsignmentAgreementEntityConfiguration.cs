using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryConsignmentAgreementEntityConfiguration
    : IEntityTypeConfiguration<CargoDryConsignmentAgreementEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryConsignmentAgreementEntity> builder)
    {
        builder.ToTable("consignment_agreements");
        builder.HasKey(x => x.Id);

        // ── Core identity ──────────────────────────────────────────────────────
        builder.Property(x => x.AgreementCode).HasMaxLength(60).IsRequired();
        builder.HasIndex(x => x.AgreementCode).IsUnique();

        builder.Property(x => x.ProviderProfileId).IsRequired();
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();

        // ── Commercial terms ───────────────────────────────────────────────────
        builder.Property(x => x.ConsignmentRate)
            .HasColumnType("decimal(6,4)").IsRequired();   // e.g. 0.2500

        builder.Property(x => x.MinimumSettlementAmount)
            .HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasMaxLength(5).IsRequired().HasDefaultValue("TRY");

        builder.Property(x => x.MaxKitCount).IsRequired();
        builder.Property(x => x.AllocatedKitCount).IsRequired().HasDefaultValue(0);

        // ── Status & lifecycle ─────────────────────────────────────────────────
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.StartDateUtc).IsRequired();
        builder.Property(x => x.EndDateUtc);

        // ── Optional metadata ──────────────────────────────────────────────────
        builder.Property(x => x.TermsDocumentRef).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        // ── Status timestamps ──────────────────────────────────────────────────
        builder.Property(x => x.ActivatedAtUtc);
        builder.Property(x => x.SuspendedAtUtc);
        builder.Property(x => x.TerminatedAtUtc);
        builder.Property(x => x.SuspendReason).HasMaxLength(500);
        builder.Property(x => x.TerminationReason).HasMaxLength(500);

        // ── Computed property — NOT mapped ─────────────────────────────────────
        builder.Ignore(x => x.RemainingKitCount);

        // ── Indexes ────────────────────────────────────────────────────────────
        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasIndex(x => x.ProductCode);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.ProviderProfileId, x.ProductCode, x.Status });
        builder.HasIndex(x => x.StartDateUtc);
        builder.HasIndex(x => x.EndDateUtc);
        // CreateDate / ModifyDate: from AizenEntityWithAudit
    }
}

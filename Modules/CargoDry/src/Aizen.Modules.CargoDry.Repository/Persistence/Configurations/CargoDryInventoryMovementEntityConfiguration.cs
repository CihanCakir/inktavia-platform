using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryInventoryMovementEntityConfiguration
    : IEntityTypeConfiguration<CargoDryInventoryMovementEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryInventoryMovementEntity> builder)
    {
        builder.ToTable("inventory_movements");
        builder.HasKey(x => x.Id);

        // ── Context ────────────────────────────────────────────────────────────
        builder.Property(x => x.ProviderProfileId).IsRequired();
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BatchCode).HasMaxLength(100);
        builder.Property(x => x.KitId);

        // ── Movement ───────────────────────────────────────────────────────────
        builder.Property(x => x.MovementType).HasConversion<int>().IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.BalanceAfter);

        // ── Commercial context ─────────────────────────────────────────────────
        builder.Property(x => x.CommercialModel).HasConversion<int>();
        builder.Property(x => x.SalesChannel).HasConversion<int>();

        // ── Reference ─────────────────────────────────────────────────────────
        builder.Property(x => x.ReferenceType).HasMaxLength(100);
        builder.Property(x => x.ReferenceId);
        builder.Property(x => x.Note).HasMaxLength(500);

        // ── Audit ──────────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedByUserId);

        // ── Indexes ────────────────────────────────────────────────────────────
        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasIndex(x => x.ProductCode);
        builder.HasIndex(x => x.BatchCode);
        builder.HasIndex(x => x.KitId);
        builder.HasIndex(x => x.MovementType);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(new[] { nameof(CargoDryInventoryMovementEntity.ProviderProfileId),
                                 nameof(CargoDryInventoryMovementEntity.ProductCode) });
    }
}

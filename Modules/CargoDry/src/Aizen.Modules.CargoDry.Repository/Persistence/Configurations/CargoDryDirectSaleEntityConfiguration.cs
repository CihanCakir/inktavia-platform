using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryDirectSaleEntityConfiguration : IEntityTypeConfiguration<CargoDryDirectSaleEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryDirectSaleEntity> builder)
    {
        builder.ToTable("direct_sales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceServiceRequestId).IsRequired();
        builder.HasIndex(x => x.SourceServiceRequestId).IsUnique(); // one direct-sale record per cargo order (idempotency)

        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SaleAmount).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.KitId);
        builder.Property(x => x.TrackingCode).HasMaxLength(100);
        builder.Property(x => x.ShippedAtUtc);
        builder.Property(x => x.CompletedAtUtc).IsRequired();

        builder.HasIndex(x => x.ProductCode);
        builder.HasIndex(x => x.CompletedAtUtc);
    }
}

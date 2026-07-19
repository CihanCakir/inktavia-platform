using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryStockRequestEntityConfiguration : IEntityTypeConfiguration<CargoDryStockRequestEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryStockRequestEntity> builder)
    {
        builder.ToTable("cargodry_stock_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RequestCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ProductCode).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ProviderNote).HasMaxLength(500);
        builder.Property(x => x.DecisionNote).HasMaxLength(1000);
        builder.Property(x => x.ApprovedBatchCode).HasMaxLength(100);
        builder.HasIndex(x => x.RequestCode).IsUnique();
        builder.HasIndex(x => new { x.ProviderProfileId, x.Status });
    }
}

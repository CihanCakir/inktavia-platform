using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryBatchEntityConfiguration : IEntityTypeConfiguration<CargoDryBatchEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryBatchEntity> builder)
    {
        builder.ToTable("batches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BatchCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.BatchCode).IsUnique();
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.KitCount).IsRequired();
        builder.Property(x => x.IsRevoked).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.RevokeReason).HasMaxLength(500);
        builder.Property(x => x.QrZipFileRef).HasMaxLength(500);
        builder.Property(x => x.ExcelFileRef).HasMaxLength(500);
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.CreatedByAdminId).IsRequired();
        builder.Property(x => x.BatchLabel).HasMaxLength(200);
        builder.Property(x => x.WarehouseCode).HasMaxLength(50);
        builder.Property(x => x.ProductionNotes).HasMaxLength(2000);
        // Indexes for stats queries and filtering
        builder.HasIndex(x => x.ProductCode);
        builder.HasIndex(x => x.IsRevoked);
        // CreateDate / ModifyDate: from AizenEntityWithAudit — DO NOT re-map
    }
}
